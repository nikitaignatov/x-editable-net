using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using InlineEdit.Core.Commands;
using InlineEdit.Core.Events;
using InlineEdit.Core.Model;

namespace InlineEdit.Core
{
    public interface IInlineEdit<T> : IDisposable
    {
        IEnumerable<IMessage> Handle(UpdateEntityCommand<T> cmd);
        event EventHandler<EntityUpdatedEventArgs<T>> EntityUpdated;
        event EventHandler<EntityUpdatedEventArgs<T>> Complete;
        event EventHandler<ExceptionEventArgs<T>> Exception;
        event EventHandler<SetPropertyExceptionEventArgs<T>> SetPropertyException;
    }

    public interface IInlineEdit<T, TContext> : IInlineEdit<T>
        where TContext : DbContext
    {
        event EventHandler<BeforeSaveEventArgs<T, TContext>> BeforeSave;
        event EventHandler<DbEntityValidationExceptionEventArgs<T>> DbEntityValidationException;
    }

    public class InlineEdit<T, TContext> : IInlineEdit<T, TContext>
        where TContext : DbContext
    {
        private readonly TContext db;
        private const int defaultVersion = -1;

        public InlineEdit(TContext db)
        {
            this.db = db;
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public IEnumerable<IMessage> Handle(UpdateEntityCommand<T> cmd)
        {
            using (var tx = db.Database.BeginTransaction(cmd.IsolationLevel))
            {
                return Handle(cmd, tx);
            }
        }

        public IEnumerable<IMessage> Handle(UpdateEntityCommand<T> cmd, DbContextTransaction tx)
        {
            var item = db.Set(cmd.EntityType).Find(cmd.Id);
            if (item == null)
            {
                throw new NullReferenceException("Resource not found");
            }

            var version = defaultVersion;

            try
            {
                var original = UpdateEntity(cmd, item);
                version = UpdateVersion(item);
                OnBeforeSave(new BeforeSaveEventArgs<T, TContext>
                {
                    Context = db,
                    Command = cmd,
                    Original = original.From,
                    Version = version
                });
                db.SaveChanges();

                tx.Commit();

                OnEntityUpdated(new EntityUpdatedEventArgs<T>
                {
                    Command = cmd,
                    Original = new { value = original, version },
                    Version = version
                });
                return new List<IMessage>();
            }
            catch (DbEntityValidationException ex)
            {
                tx.Rollback();
                OnDbEntityValidationException(new DbEntityValidationExceptionEventArgs<T>(cmd, ex, version));
                throw;
            }
            catch (Exception ex)
            {
                tx.Rollback();
                OnException(new ExceptionEventArgs<T>(cmd, ex, version));
                throw;
            }
            finally
            {
                OnComplete(new EntityUpdatedEventArgs<T>
                {
                    Command = cmd,
                    Version = version
                });
            }
        }

        private UpdatedValue<object> UpdateEntity(UpdateEntityCommand<T> cmd, object item)
        {
            var original = SetPropertyValue(item, cmd.Name, cmd.Value);
            if (original == null)
            {
                throw new NullReferenceException("Could not update property");
            }

            if (original.From?.ToString() == original.To?.ToString())
            {
                throw new Exception("Same value as the original");
            }

            db.Entry(item).State = EntityState.Detached;
            db.Set(cmd.EntityType).Attach(item);
            db.Entry(item).Property(cmd.Name).IsModified = true;
            return original;
        }

        private static int UpdateVersion(object item)
        {
            var versioned = item as IHaveVersion;
            if (versioned == null)
            {
                throw new Exception("Item is not versionable");
            }

            var v = versioned.Version;
            versioned.Version = v + 1;
            return versioned.Version;
        }

        public event EventHandler<BeforeSaveEventArgs<T, TContext>> BeforeSave;
        public event EventHandler<EntityUpdatedEventArgs<T>> EntityUpdated;
        public event EventHandler<EntityUpdatedEventArgs<T>> Complete;
        public event EventHandler<DbEntityValidationExceptionEventArgs<T>> DbEntityValidationException;
        public event EventHandler<ExceptionEventArgs<T>> Exception;
        public event EventHandler<SetPropertyExceptionEventArgs<T>> SetPropertyException;

        protected virtual void OnBeforeSave(BeforeSaveEventArgs<T, TContext> e) => BeforeSave?.Invoke(this, e);
        protected virtual void OnEntityUpdated(EntityUpdatedEventArgs<T> e) => EntityUpdated?.Invoke(this, e);
        protected virtual void OnComplete(EntityUpdatedEventArgs<T> e) => Complete?.Invoke(this, e);
        protected virtual void OnDbEntityValidationException(DbEntityValidationExceptionEventArgs<T> e) => DbEntityValidationException?.Invoke(this, e);
        protected virtual void OnException(ExceptionEventArgs<T> e) => Exception?.Invoke(this, e);
        protected virtual void OnSetPropertyException(SetPropertyExceptionEventArgs<T> e) => SetPropertyException?.Invoke(this, e);

        private UpdatedValue<object> SetPropertyValue(object entity, string property, string value)
        {
            var propertyInfo = entity.GetType().GetProperty(property);
            if (propertyInfo == null) return null;

            try
            {
                var original = propertyInfo.GetValue(entity);
                var newValue = GetValue(value, propertyInfo.PropertyType);
                propertyInfo.SetValue(entity, newValue, null);
                return new UpdatedValue<object>(original, newValue);
            }
            catch (Exception ex)
            {
                OnSetPropertyException(new SetPropertyExceptionEventArgs<T>
                {
                    Exception = new Exception($"Failed to set value for {entity}[{property}][{value}]", ex),
                    Property = property,
                    Value = value
                });
                return null;
            }
        }

        private object GetValue(string value, Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return string.IsNullOrWhiteSpace(value) ? null : GetValue(value, t.GetGenericArguments()[0]);
            }
            if (t == typeof(Guid))
            {
                return new Guid(value);
            }
            if (t.IsEnum)
            {
                return Enum.Parse(t, value);
            }
            if (t == typeof(DateTime))
            {
                return DateTime.Parse(value);
            }
            return Convert.ChangeType(value, t);
        }
    }
}
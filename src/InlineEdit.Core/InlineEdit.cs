using System;
using System.Data.Entity;
using System.Data.Entity.Validation;
using InlineEdit.Core.Commands;
using InlineEdit.Core.Events;
using InlineEdit.Core.Model;

namespace InlineEdit.Core
{
    public interface IInlineEdit<T> : IDisposable
    {
        UpdateEntityCommand<T> Handle(UpdateEntityCommand<T> cmd);
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
        private const int DefaultVersion = -1;

        public InlineEdit(TContext db)
        {
            this.db = db;
        }

        /// <summary>
        /// Handles the command provided to the method, will update the DbSet based on the EntitySet provided in the command.
        /// New tranaction will be created, where Isolation level that is provided on the command will be used.
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public UpdateEntityCommand<T> Handle(UpdateEntityCommand<T> cmd)
        {
            using (var tx = db.Database.BeginTransaction(cmd.IsolationLevel))
            {
                return Handle(cmd, tx);
            }
        }

        /// <summary>
        /// Handles the command provided to the method, will update the DbSet based on the EntitySet provided in the command.
        /// This overload allows to supply own transaction reference.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="tx"></param>
        /// <returns></returns>
        public UpdateEntityCommand<T> Handle(UpdateEntityCommand<T> cmd, DbContextTransaction tx)
        {
            var item = db.Set(cmd.EntityType).Find(cmd.Id);
            if (item == null)
            {
                throw new NullReferenceException("Resource not found");
            }

            var version = DefaultVersion;

            try
            {
                var original = UpdateEntity(cmd, item);
                version = UpdateVersion(item as IHaveVersion, DefaultVersion);
                BeforeSave?.Invoke(this, new BeforeSaveEventArgs<T, TContext>
                {
                    Context = db,
                    Command = cmd,
                    Original = original.From,
                    Version = version
                });
                db.SaveChanges();

                tx.Commit();
                return cmd;
            }
            catch (DbEntityValidationException ex)
            {
                tx.Rollback();
                DbEntityValidationException?.Invoke(this, new DbEntityValidationExceptionEventArgs<T>(cmd, ex, version));
                throw;
            }
            catch (Exception ex)
            {
                tx.Rollback();
                Exception?.Invoke(this, new ExceptionEventArgs<T>(cmd, ex, version));
                throw;
            }
            finally
            {
                Complete?.Invoke(this, new EntityUpdatedEventArgs<T>
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

        private static int UpdateVersion(IHaveVersion item, int defaultVersion) => item?.Version + 1 ?? defaultVersion;

        public event EventHandler<BeforeSaveEventArgs<T, TContext>> BeforeSave;
        public event EventHandler<EntityUpdatedEventArgs<T>> Complete;
        public event EventHandler<DbEntityValidationExceptionEventArgs<T>> DbEntityValidationException;
        public event EventHandler<ExceptionEventArgs<T>> Exception;
        public event EventHandler<SetPropertyExceptionEventArgs<T>> SetPropertyException;

        private UpdatedValue<object> SetPropertyValue(object entity, string property, string value)
        {
            var propertyInfo = entity.GetType().GetProperty(property);
            if (propertyInfo == null) return null;

            try
            {
                var original = propertyInfo.GetValue(entity);
                var newValue = ValueConverter.Convert(value, propertyInfo.PropertyType);
                propertyInfo.SetValue(entity, newValue, null);
                return new UpdatedValue<object>(original, newValue);
            }
            catch (Exception ex)
            {
                SetPropertyException?.Invoke(this, new SetPropertyExceptionEventArgs<T>
                {
                    Exception = new Exception($"Failed to set value for {entity}[{property}][{value}]", ex),
                    Property = property,
                    Value = value
                });
                return null;
            }
        }

        public void Dispose()
        {
            db.Dispose();
        }
    }
}
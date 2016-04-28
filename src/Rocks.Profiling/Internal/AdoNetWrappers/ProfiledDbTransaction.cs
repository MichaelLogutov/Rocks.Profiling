using System;
using System.Data;
using System.Data.Common;
using JetBrains.Annotations;

namespace Rocks.Profiling.Internal.AdoNetWrappers
{
    internal class ProfiledDbTransaction : DbTransaction
    {
        public ProfiledDbTransaction([NotNull] DbTransaction innerTransaction, [NotNull] ProfiledDbConnection innerConnection)
        {
            if (innerTransaction == null)
                throw new ArgumentNullException(nameof(innerTransaction));

            if (innerConnection == null)
                throw new ArgumentNullException(nameof(innerConnection));

            this.InnerTransaction = innerTransaction;
            this.InnerConnection = innerConnection;
        }


        public DbTransaction InnerTransaction { get; }
        public ProfiledDbConnection InnerConnection { get; }


        public override void Commit() => this.InnerTransaction.Commit();
        public override void Rollback() => this.InnerTransaction.Rollback();
        protected override DbConnection DbConnection => this.InnerConnection;
        public override IsolationLevel IsolationLevel => this.InnerTransaction.IsolationLevel;


        protected override void Dispose(bool disposing)
        {
            if (disposing)
                this.InnerTransaction.Dispose();

            base.Dispose(disposing);
        }
    }
}
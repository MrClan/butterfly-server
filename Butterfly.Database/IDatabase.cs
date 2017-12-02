﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using Butterfly.Database.Event;

using Dict = System.Collections.Generic.Dictionary<string, object>;
using Butterfly.Database.Dynamic;

namespace Butterfly.Database {
    /// <summary>
    /// Allows executing INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data
    /// change events both on tables and dynamic views.<para/>
    /// 
    /// Adding records and echoing all data change events to the console...<para/>
    /// <code>
    ///     // Create database instance (will also read the schema from the database)
    ///     var database = new SomeDatabase();
    ///     
    ///     // Listen for all database data events
    ///     var databaseListener = database.OnNewCommittedTransaction(dataEventTransaction => {
    ///         console.WriteLine($"Low Level DataEventTransaction={dataEventTransaction}");
    ///     });
    ///     
    ///     // INSERT a couple of records (this will cause a single data even transaction with
    ///     // two INSERT data events to be written to the console above)
    ///     using (var transaction = database.BeginTransaction()) {
    ///         await database.InsertAndCommitAsync("employee", values: {
    ///             department_id: 1,
    ///             name: "SpongeBob"
    ///         });
    ///         await database.InsertAndCommitAsync("employee", values: {
    ///             department_id: 1,
    ///             name: "Squidward"
    ///         });
    ///         await database.CommitAsync();
    ///     );
    /// </code>
    /// 
    /// Creating a DynamicView and echoing data change events on the DynamicView to the console...<para/>
    /// <code>
    ///     // Create database instance (will also read the schema from the database)
    ///     var database = new SomeDatabase();
    ///     
    ///     // Create a DynamicViewSet that print any data events to the console
    ///     // (this will immediately echo an INITIAL data event for each existing matching record)
    ///     var dynamicViewSet = database.CreateAndStartDynamicViewSet(
    ///         "SELECT * FROM employee WHERE department_id=@departmentId", 
    ///         new {
    ///             departmentId = 1
    ///         },
    ///         dataEventTransaction => {
    ///             Console.WriteLine(dataEventTransaction);
    ///         }
    ///     );
    /// 
    ///     // This will cause the above DynamicViewSet to echo an INSERT data event
    ///     await database.InsertAndCommitAsync("employee", values: {
    ///         department_id: 1
    ///         name: "Mr Crabs"
    ///     });
    ///     
    ///     // This will NOT cause the above DynamicViewSet to echo an INSERT data event
    ///     // (because the department_id doesn't match)
    ///     await database.InsertAndCommitAsync("employee", values: {
    ///         department_id: 2
    ///         name: "Patrick Star"
    ///     });
    /// </code>
    /// </summary>
    public interface IDatabase {

        /// <summary>
        /// Dictionary of tables keyed by name
        /// </summary>
        Dictionary<string, Table> Tables { get; }

        /// <summary>
        /// Creates database tables from an embedded resource file by internally calling CreateFromTextAsync with the contents of the embedded resource file (<see cref="CreateFromTextAsync(string)"/>.
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="resourceFile"></param>
        /// <returns></returns>
        Task CreateFromResourceFileAsync(Assembly assembly, string resourceFile);

        /// <summary>
        /// Creates database tables from a string containing a semicolon delimited series of CREATE statements in MySQL format (will be converted to native database format as appropriate).<para/>
        /// Comments (lines beginning with --) will be ignored.<para/>
        /// Each CREATE statement must include a PRIMARY KEY definition.<para/>
        /// If the table already exists, the CREATE statement is ignored.<para/>
        /// Creating your database tables with this method is not required to use the rest of the Butterfly framework (you can instead just load your schema from your existing database using <see cref="LoadSchemaAsync"/>.
        /// </summary>
        /// <param name="sql"></param>
        Task CreateFromTextAsync(string sql);

        /// <summary>
        /// Adds a listener that is invoked when there is a new uncommitted transaction
        /// </summary>
        /// <param name="listener">The lambda to call when there is a new uncommitted <see cref="DataEventTransaction"/></param>
        /// <returns>An <see cref="IDisposable"/> that allows removing the listener by calling Dispose()</returns>
        IDisposable OnNewUncommittedTransaction(Action<DataEventTransaction> listener);

        /// <summary>
        /// Adds a listener that is invoked when there is a new committed transaction
        /// </summary>
        /// <param name="listener">The lambda to call when there is a new committed <see cref="DataEventTransaction"/></param>
        /// <returns>An <see cref="IDisposable"/> that allows removing the listener by calling Dispose()</returns>
        IDisposable OnNewCommittedTransaction(Action<DataEventTransaction> listener);

        /// <summary>
        /// Execute the SELECT statement and return the data in a <see cref="DataEventTransaction"/>
        /// </summary>
        /// <param name="sql">A SELECT statement defining what data to return (can include parameters like @name)</param>
        /// <param name="vars">Either an anonymous type or Dictionary with the vars used in the SELECT statement</param>
        /// <returns>A <see cref="DataEventTransaction"/> with the returned data represented as a sequence of <see cref="DataEvent"/></returns>
        Task<DataEventTransaction> GetInitialDataEventTransactionAsync(string sql, dynamic vars = null);

        /// <summary>
        /// Executes the SELECT statement of the DynamicQuery and returns a sequence of DataChange events starting an InitialBegin event, then an Insert event for each row, and then an InitialEnd event.
        /// </summary>
        /// <returns></returns>
        Task<DataEvent[]> GetInitialDataEventsAsync(string dataEventName, string[] keyFieldNames, SelectStatement selectStatement, dynamic statementParams = null);

        /// <summary>
        /// Executes the SELECT statement and return the value of the first column of the first row (the SELECT statement may contain vars like @name specified in the vars parameter)
        /// </summary>
        /// <typeparam name="T">The return type of the single value returned</typeparam>
        /// <param name="sql">The SELECT statement to execute (may contain vars like @name specified in the vars parameter)</param>
        /// <param name="vars">Either an anonymous type or Dictionary with the vars used in the SELECT statement</param>
        /// <param name="defaultValue">The value to return if no rows were returned or the value of the first column of the first row is null</param>
        /// <returns>The value of the first column of the first row</returns>
        Task<T> SelectValue<T>(string sql, dynamic vars, T defaultValue);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="vars"></param>
        /// <returns></returns>
        Task<Dict> SelectRowAsync(string sql, dynamic vars = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="vars"></param>
        /// <returns></returns>
        Task<Dict[]> SelectRowsAsync(string sql, dynamic vars = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="vars"></param>
        /// <param name="ignoreIfDuplicate"></param>
        /// <returns></returns>
        Task<object> InsertAndCommitAsync(string sql, dynamic vars, bool ignoreIfDuplicate = false);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="vars"></param>
        /// <returns></returns>
        Task<int> UpdateAndCommitAsync(string sql, dynamic vars);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="vars"></param>
        /// <returns></returns>
        Task<int> DeleteAndCommitAsync(string sql, dynamic vars);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<ITransaction> BeginTransaction();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="getDefaultValue"></param>
        /// <param name="tableName"></param>
        void SetDefaultValue(string fieldName, Func<object> getDefaultValue, string tableName = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="listenerDataEventFilter"></param>
        /// <returns></returns>
        DynamicViewSet CreateDynamicViewSet(Action<DataEventTransaction> listener, Func<DataEvent, bool> listenerDataEventFilter = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="asyncListener"></param>
        /// <param name="listenerDataEventFilter"></param>
        /// <returns></returns>
        DynamicViewSet CreateDynamicViewSet(Func<DataEventTransaction, Task> asyncListener, Func<DataEvent, bool> listenerDataEventFilter = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="listener"></param>
        /// <param name="values"></param>
        /// <param name="name"></param>
        /// <param name="keyFieldNames"></param>
        /// <param name="listenerDataEventFilter"></param>
        /// <returns></returns>
        Task<DynamicViewSet> CreateAndStartDynamicView(string sql, Action<DataEventTransaction> listener, dynamic values = null, string name = null, string[] keyFieldNames = null, Func<DataEvent, bool> listenerDataEventFilter = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="asyncListener"></param>
        /// <param name="values"></param>
        /// <param name="name"></param>
        /// <param name="keyFieldNames"></param>
        /// <param name="listenerDataEventFilter"></param>
        /// <returns></returns>
        Task<DynamicViewSet> CreateAndStartDynamicView(string sql, Func<DataEventTransaction, Task> asyncListener, dynamic values = null, string name = null, string[] keyFieldNames = null, Func<DataEvent, bool> listenerDataEventFilter = null);
    }
}

﻿using EvidentInstruction.Controllers;
using EvidentInstruction.Database.Controllers;
using EvidentInstruction.Database.Models;
using EvidentInstruction.Database.Steps;
using EvidentInstruction.Database.Exceptions;
using FluentAssertions;
using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;
using EvidentInstruction.Database.Models.Interfaces;
using Moq;
using EvidentInstruction.Database.Infrastructures;
using System.Data;
using TechTalk.SpecFlow;

namespace EvidentInstruction.Database.Tests
{
    [ExcludeFromCodeCoverage]
    public class SqlServerStepTests
    {
        private DbConnectionParams dbConnectionParams;
        private const string dbConnectionString = "TestConnectionString";
        private DatabaseController databaseController;
        private VariableController variableController;
        private SqlServerSteps step;


        public SqlServerStepTests()
        {
            dbConnectionParams = new DbConnectionParams() { Database = "Test", Source = "test", Login = "test", Password = "W9qNIafQbJCZzEafUaYmQw==" };
            databaseController = new DatabaseController();
            variableController = new VariableController();
            step = new SqlServerSteps(databaseController, variableController);
        }

        [Fact]
        public void GetDataBaseParametersFromTableSqlServer_CorrectTable_ReturnDbConnectionParams()
        {
            var table = new Table(new string[] { "Source", "Database", "Login", "Password" });
            table.AddRow("Db1", "Test", "User", "W9qNIafQbJCZzEafUaYmQw==");

            var result = step.GetDataBaseParametersFromTableSqlServer(table);

            result.Should().NotBeNull();
            result.Login.Should().Be("User");
            result.Source.Should().Be("Db1");
            result.Database.Should().Be("Test");
            result.Password.Should().Be("test");
        }

        [Fact]
        public void TransformationTableToString_CorrectTable_ReturnIEnumerable()
        {
            var table = new Table(new string[] { "id", "User", "Balance", "Date" });
            table.AddRow("1234", "Иван Иванов", "10000.10", "10.10.2000");
            table.AddRow("1243", "Петор Петров", "100000", "2000-12-12");

            var result = step.TransformationTableToString(table);

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        [Fact]
        public void TransformationTableToString_TableIsEmpty_ReturnThrow()
        {
            var table = new Table(new string[] { "id", "User", "Balance", "Date" });

            Action action = () => step.TransformationTableToString(table);
            action.Should()
                .Throw<ArgumentNullException>()
                .WithMessage($"Value cannot be null.{Environment.NewLine}Parameter name: List with table patameters is Empty.");
        }

        [Fact]
        public void ConnectToDB_SqlServer_IncorrectDbParams_ReturnThrow()
        {
            Action action = () => step.ConnectToDB_SqlServer(dbConnectionString, dbConnectionParams);
            action.Should()
                .Throw<ConnectSqlException>()
                .WithMessage($"Connection failed. Connection with parameters: {Database.Helpers.Message.CreateMessage(dbConnectionParams)}" +
                " A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server is configured to allow remote connections. (provider: Named Pipes Provider, error: 40 - Could not open a connection to SQL Server)");
        }

        [Fact]
        public void ConnectToDB_SqlServer_DbParamsIsNull_ReturnThrow()
        {
            dbConnectionParams = new DbConnectionParams() { Database = "", Source = "", Login = "", Password = "", Timeout = 0 };

            Action action = () => step.ConnectToDB_SqlServer(dbConnectionString, dbConnectionParams);
            action.Should()
                .Throw<Xunit.Sdk.XunitException>();
        }

        [Fact]
        public void ExecuteQuery_IncorrectParams_ReturnNull()
        {
            var mockSqlProvider = new Mock<IDbClient>();           
            var connectName = "NewConnect";
            var query = "SELECT top 100 * test111";

            IDbClient connection = new SqlServerClient();
            mockSqlProvider.Setup(c => c.Create(It.IsAny<DbConnectionParams>())).Returns(true);

            connection = mockSqlProvider.Object;

            this.databaseController.Connections.TryAdd(connectName, (connection, 30));

            step.ExecuteQuery(QueryType.SELECT, connectName, query);
            // TODO check
        }

        [Fact]
        public void ExecuteQuery_CorrectParams_ReturnNewVariable()
        {
            var mockSqlProvider = new Mock<IDbClient>();
            var varName = "newVariable";
            var connectName = "NewConnect";
            var query = "INSERT INTO test111 (f1) VALUES (1) ";


            IDbClient connection = new SqlServerClient();
            mockSqlProvider.Setup(c => c.Create(It.IsAny<DbConnectionParams>())).Returns(true);

            connection = mockSqlProvider.Object;

            this.databaseController.Connections.TryAdd(connectName, (connection, 30));

            step.ExecuteQuery(QueryType.INSERT, connectName, varName, query);

            this.variableController.Variables.Should().NotBeEmpty();
        }
    }
}
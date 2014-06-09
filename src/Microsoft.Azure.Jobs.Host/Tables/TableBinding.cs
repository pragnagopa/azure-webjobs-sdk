﻿using System;
using System.IO;
using Microsoft.Azure.Jobs.Host.Bindings;
using Microsoft.Azure.Jobs.Host.Converters;
using Microsoft.Azure.Jobs.Host.Protocols;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Azure.Jobs.Host.Tables
{
    internal class TableBinding : IBinding
    {
        private readonly string _parameterName;
        private readonly IArgumentBinding<CloudTable> _argumentBinding;
        private readonly CloudTableClient _client;
        private readonly string _accountName;
        private readonly string _tableName;
        private readonly IObjectToTypeConverter<CloudTable> _converter;

        public TableBinding(string parameterName, IArgumentBinding<CloudTable> argumentBinding, CloudTableClient client,
            string tableName)
        {
            _parameterName = parameterName;
            _argumentBinding = argumentBinding;
            _client = client;
            _accountName = TableClient.GetAccountName(client);
            _tableName = tableName;
            _converter = CreateConverter(client, tableName);
        }

        public bool FromAttribute
        {
            get { return true; }
        }

        public string TableName
        {
            get { return _tableName; }
        }

        private FileAccess Access
        {
            get
            {
                return _argumentBinding.ValueType == typeof(CloudTable)
                    ? FileAccess.ReadWrite : FileAccess.Read;
            }
        }

        private static IObjectToTypeConverter<CloudTable> CreateConverter(CloudTableClient client, string tableName)
        {
            return new CompositeObjectToTypeConverter<CloudTable>(
                new OutputConverter<CloudTable>(new IdentityConverter<CloudTable>()),
                new OutputConverter<string>(new StringToCloudTableConverter(client, tableName)));
        }

        public IValueProvider Bind(BindingContext context)
        {
            string resolvedTableName = RouteParser.ApplyBindingData(_tableName, context.BindingData);
            TableClient.ValidateAzureTableName(resolvedTableName);
            CloudTable table = _client.GetTableReference(resolvedTableName);

            return Bind(table, context);
        }

        private IValueProvider Bind(CloudTable value, ArgumentBindingContext context)
        {
            return _argumentBinding.Bind(value, context);
        }

        public IValueProvider Bind(object value, ArgumentBindingContext context)
        {
            CloudTable table = null;

            if (!_converter.TryConvert(value, out table))
            {
                throw new InvalidOperationException("Unable to convert value to CloudTable.");
            }

            return Bind(table, context);
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new TableParameterDescriptor
            {
                Name = _parameterName,
                AccountName = _accountName,
                TableName = _tableName,
                Access = Access
            };
        }
    }
}
﻿using System.Collections.Generic;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.QueryBuilders.RecordSetter.Internal;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Update;
using SqExpress.Utils;

namespace SqExpress.QueryBuilders.Insert.Internal
{
    public class InsertDataBuilder<TTable, TItem> : IInsertDataBuilder<TTable, TItem> where TTable : ExprTable
    {
        readonly TTable _target;

        private readonly IEnumerable<TItem> _data;

        private DataMapping<TTable, TItem>? _dataMapping;

        private TargetInsertSelectMapping<TTable>? _targetInsertSelectMapping;

        private IReadOnlyList<ExprAliasedColumnName>? _output;

        public InsertDataBuilder(TTable target, IEnumerable<TItem> data)
        {
            this._target = target;
            this._data = data;
        }

        public IInsertDataBuilderAlsoInsert<TTable> MapData(DataMapping<TTable, TItem> mapping)
        {
            this._dataMapping.AssertFatalNull(nameof(this._dataMapping));
            this._dataMapping = mapping;
            return this;
        }

        public IInsertDataBuilderMapOutput AlsoInsert(TargetInsertSelectMapping<TTable> targetInsertSelectMapping)
        {
            this._targetInsertSelectMapping.AssertFatalNull(nameof(this._dataMapping));
            this._targetInsertSelectMapping = targetInsertSelectMapping;
            return this;
        }

        public ExprInsert Done()
        {
            var useDerivedTable = this._targetInsertSelectMapping != null;

            var mapping =  this._dataMapping.AssertFatalNotNull(nameof(this._dataMapping));

            int? capacity = this._data.TryToCheckLength(out var c) ? c : (int?)null;

            if (capacity != null && capacity.Value < 1)
            {
                throw new SqExpressException("Input data should not be empty");
            }


            List<ExprValueRow>? recordsS = null;
            List<ExprInsertValueRow>? recordsI = null;

            if (useDerivedTable)
            {
                recordsS = capacity.HasValue ? new List<ExprValueRow>(capacity.Value) : new List<ExprValueRow>();
            }
            else
            {
                recordsI = capacity.HasValue ? new List<ExprInsertValueRow>(capacity.Value) : new List<ExprInsertValueRow>();
            }

            DataMapSetter<TTable, TItem>? dataMapSetter = null;
            IReadOnlyList<ExprColumnName>? columns = null;

            foreach (var item in this._data)
            {
                dataMapSetter ??= new DataMapSetter<TTable, TItem>(this._target, item);

                dataMapSetter.NextItem(item, columns?.Count);
                mapping(dataMapSetter);

                columns ??= dataMapSetter.Columns;

                dataMapSetter.EnsureRecordLength();

                recordsS?.Add(new ExprValueRow(dataMapSetter.Record.AssertFatalNotNull(nameof(dataMapSetter.Record))));
                recordsI?.Add(new ExprInsertValueRow(dataMapSetter.Record.AssertFatalNotNull(nameof(dataMapSetter.Record))));
            }

            if ( (recordsS?.Count ?? 0 + recordsI?.Count ?? 0) < 1 || columns == null)
            {
                //In case of empty IEnumerable
                throw new SqExpressException("Input data should not be empty");
            }

            IExprInsertSource insertSource;

            if (recordsI != null)
            {
                insertSource = new ExprInsertValues(recordsI);
            }
            else if(recordsS != null && this._targetInsertSelectMapping != null)
            {
                var valuesConstructor = new ExprTableValueConstructor(recordsS);
                var values = new ExprDerivedTableValues(
                    valuesConstructor,
                    new ExprTableAlias(Alias.Auto.BuildAliasExpression().AssertNotNull("Alias cannot be null")),
                    columns);

                var targetUpdateSetter = new TargetInsertSelectSetter<TTable>(this._target);
                this._targetInsertSelectMapping.Invoke(targetUpdateSetter);

                var maps = targetUpdateSetter.Maps;
                if (maps.Count < 1)
                {
                    throw new SqExpressException("Additional insertion cannot be null");
                }

                var selectValues = new List<IExprSelecting>(columns.Count + maps.Count);

                foreach (var exprColumnName in values.Columns)
                {
                    selectValues.Add(exprColumnName);
                }
                foreach (var m in maps)
                {
                    selectValues.Add(m.Value);
                }

                var query = SqQueryBuilder.Select(selectValues).From(values).Done();
                insertSource = new ExprInsertQuery(query);

                var extraInsertCols = maps.SelectToReadOnlyList(m => m.Column);

                columns = Helpers.Combine(columns, extraInsertCols);

            }
            else
            {
                //Actually C# should have detected that this brunch cannot be invoked
                throw new SqExpressException("Fatal logic error!");
            }

            return new ExprInsert(this._target.FullName, columns, insertSource);
        }

        public IInsertDataBuilderFinalOutput Output(ExprAliasedColumnName column, params ExprAliasedColumnName[] rest)
        {
            this._output.AssertFatalNull(nameof(this._output));
            this._output = Helpers.Combine(column, rest);
            return this;
        }

        public IInsertDataBuilderFinalOutput Output(IReadOnlyList<ExprAliasedColumnName> columns)
        {
            this._output.AssertFatalNull(nameof(this._output));
            this._output = columns;
            return this;
        }

        ExprInsertOutput IInsertDataBuilderFinalOutput.Done()
        {
            var output = this._output.AssertFatalNotNull(nameof(this._output));
            var insert = this.Done();

            return new ExprInsertOutput(insert, output);
        }

        IExprQuery IExprQueryFinal.Done()
        {
            return ((IInsertDataBuilderFinalOutput) this).Done();
        }

        IExprExec IExprExecFinal.Done()
        {
            return this.Done();
        }
    }
}
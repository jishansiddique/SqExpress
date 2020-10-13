﻿using System;
using System.Collections.Generic;
using System.Text;
using SqExpress.SqlExport.Internal;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress.SqlExport
{
    public class PgSqlBuilder : SqlBuilderBase
    {
        public PgSqlBuilder(SqlBuilderOptions? options = null, StringBuilder? externalBuilder = null) : base(options, externalBuilder)
        {
        }

        public override bool VisitExprGuidLiteral(ExprGuidLiteral exprGuidLiteral)
        {
            if (exprGuidLiteral.Value == null)
            {
                this.AppendNull();
                return true;
            }
            
            this.Builder.Append('\'');
            this.Builder.Append(exprGuidLiteral.Value.Value.ToString("D"));
            this.Builder.Append("'::uuid");

            return true;
        }

        public override bool VisitExprBoolLiteral(ExprBoolLiteral boolLiteral)
        {
            if (boolLiteral.Value.HasValue)
            {
                this.Builder.Append(boolLiteral.Value.Value ? "true" : "false");
            }
            else
            {
                this.AppendNull();
            }

            return true;
        }

        public override bool VisitExprStringConcat(ExprStringConcat exprStringConcat)
        {
            exprStringConcat.Left.Accept(this);
            this.Builder.Append("||");
            exprStringConcat.Right.Accept(this);
            return true;
        }

        protected override void AppendSelectTop(ExprValue top)
        {
            throw new NotSupportedException("PostgreSql does not support 'TOP x' expression");
        }

        public override bool VisitExprInsertOutput(ExprInsertOutput exprInsertOutput)
        {
            this.GenericInsert(exprInsertOutput.Insert,
                null,
                () =>
                {
                    exprInsertOutput.OutputColumns.AssertNotEmpty("INSERT OUTPUT cannot be empty");
                    this.Builder.Append(" RETURNING ");
                    this.AcceptListComaSeparated(exprInsertOutput.OutputColumns);
                });
            return true;
        }

        public override bool VisitExprUpdate(ExprUpdate exprUpdate)
        {
            this.AssertNotEmptyList(exprUpdate.SetClause, "'UPDATE' statement should have at least one set clause");

            this.Builder.Append("UPDATE ");
            exprUpdate.Target.Accept(this);
            this.Builder.Append(" SET ");
            for (int i = 0; i < exprUpdate.SetClause.Count; i++)
            {
                var setClause = exprUpdate.SetClause[i];
                if (i != 0)
                {
                    this.Builder.Append(',');
                }
                setClause.Column.ColumnName.Accept(this);
                this.Builder.Append('=');
                setClause.Value.Accept(this);
            }

            ExprBoolean? sourceFilter = null;
            if (exprUpdate.Source != null)
            {
                IReadOnlyList<IExprTableSource> tables;
                (tables, sourceFilter) = exprUpdate.Source.ToTableMultiplication();
                this.AssertNotEmptyList(tables, "List of tables in 'UPDATE' statement cannot be empty");
                if (tables.Count > 1)
                {
                    this.Builder.Append(" FROM ");
                    int itemAppendCount = 0;
                    for (int i = 0; i < tables.Count; i++)
                    {
                        var source = tables[i];

                        if (source is ExprTable sTable && sTable.Equals(exprUpdate.Target))
                        {
                            continue;
                        }

                        if (itemAppendCount != 0)
                        {
                            this.Builder.Append(',');
                        }

                        source.Accept(this);
                        itemAppendCount++;
                    }

                    if (itemAppendCount >= tables.Count)
                    {
                        throw new SqExpressException("Could not found target table in 'UPDATE' statement");
                    }
                }
            }

            var filter = Helpers.CombineNotNull(sourceFilter, exprUpdate.Filter, (l, r) => l & r);

            if (filter != null)
            {
                this.Builder.Append(" WHERE ");
                filter.Accept(this);
            }
            return true;
        }

        public override bool VisitExprDelete(ExprDelete exprDelete)
        {
            this.Builder.Append("DELETE FROM ");
            exprDelete.Target.Accept(this);

            ExprBoolean? sourceFilter = null;
            if (exprDelete.Source != null)
            {
                IReadOnlyList<IExprTableSource> tables;
                (tables, sourceFilter) = exprDelete.Source.ToTableMultiplication();

                this.AssertNotEmptyList(tables, "List of tables in 'DELETE' statement cannot be empty");

                if (tables.Count > 1)
                {
                    this.Builder.Append(" USING ");
                    int itemAppendCount = 0;
                    for (int i = 0; i < tables.Count; i++)
                    {
                        var source = tables[i];

                        if (source is ExprTable sTable && sTable.Equals(exprDelete.Target))
                        {
                            continue;
                        }

                        if (itemAppendCount != 0)
                        {
                            this.Builder.Append(',');
                        }

                        source.Accept(this);
                        itemAppendCount++;
                    }

                    if (itemAppendCount >= tables.Count)
                    {
                        throw new SqExpressException("Could not found target table in 'DELETE' statement");
                    }
                }
            }

            var filter = Helpers.CombineNotNull(sourceFilter, exprDelete.Filter, (l,r) => l & r);

            if (filter != null)
            {
                this.Builder.Append(" WHERE ");
                filter.Accept(this);
            }
            return true;
        }

        public override bool VisitExprDeleteOutput(ExprDeleteOutput exprDeleteOutput)
        {
            exprDeleteOutput.Delete.Accept(this);
            this.AssertNotEmptyList(exprDeleteOutput.OutputColumns, "Output list in 'DELETE' statement cannot be empty");

            this.Builder.Append(" RETURNING ");

            this.AcceptListComaSeparated(exprDeleteOutput.OutputColumns);

            return true;
        }

        public override bool VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean)
        {
            this.Builder.Append("bool");
            return true;
        }

        public override bool VisitExprTypeByte(ExprTypeByte exprTypeByte)
        {
            throw new NotSupportedException("PostgresSQL does not support 1 byte numeric");
        }

        public override bool VisitExprTypeInt16(ExprTypeInt16 exprTypeInt16)
        {
            this.Builder.Append("int2");
            return true;
        }

        public override bool VisitExprTypeInt32(ExprTypeInt32 exprTypeInt32)
        {
            this.Builder.Append("int4");
            return true;
        }

        public override bool VisitExprTypeInt64(ExprTypeInt64 exprTypeInt64)
        {
            this.Builder.Append("int8");
            return true;
        }

        public override bool VisitExprTypeDecimal(ExprTypeDecimal exprTypeDecimal)
        {
            this.Builder.Append("decimal");
            if (exprTypeDecimal.PrecisionScale.HasValue)
            {
                this.Builder.Append('(');
                this.Builder.Append(exprTypeDecimal.PrecisionScale.Value.Precision);
                if (exprTypeDecimal.PrecisionScale.Value.Scale.HasValue)
                {
                    this.Builder.Append(',');
                    this.Builder.Append(exprTypeDecimal.PrecisionScale.Value.Scale.Value);
                }
                this.Builder.Append(')');
            }
            return true;
        }

        public override bool VisitExprTypeDouble(ExprTypeDouble exprTypeDouble)
        {
            this.Builder.Append("float8");
            return true;
        }

        public override bool VisitExprTypeDateTime(ExprTypeDateTime exprTypeDateTime)
        {
            if (exprTypeDateTime.IsDate)
            {
                this.Builder.Append("date");
            }
            else
            {
                this.Builder.Append("timestamp");
            }

            return true;
        }

        public override bool VisitExprTypeGuid(ExprTypeGuid exprTypeGuid)
        {
            this.Builder.Append("uuid");
            return true;
        }

        public override bool VisitExprTypeString(ExprTypeString exprTypeString)
        {
            this.Builder.Append("character varying");
            if (exprTypeString.Size.HasValue)
            {
                this.Builder.Append('(');
                this.Builder.Append(exprTypeString.Size.Value);
                this.Builder.Append(')');
            }
            return true;
        }

        public override bool VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull)
        {
            SqQueryBuilder.Coalesce(exprFuncIsNull.Test, exprFuncIsNull.Alt).Accept(this);
            return true;
        }

        public override bool VisitExprFuncGetDate(ExprGetDate exprGetDate)
        {
            this.Builder.Append("now()");
            return true;
        }

        public override bool VisitExprFuncGetUtcDate(ExprGetUtcDate exprGetUtcDate)
        {
            this.Builder.Append("now() at time zone 'utc'");
            return true;
        }

        public override void AppendName(string name)
        {
            this.Builder.Append("\"");
            SqlInjectionChecker.AppendStringEscapeDoubleQuote(this.Builder, name);
            this.Builder.Append("\"");
        }

        protected override void AppendUnicodePrefix(string str) { }
    }
}
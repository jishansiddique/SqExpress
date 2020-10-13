﻿using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Functions
{
    public class ExprCase : ExprValue
    {
        public ExprCase(IReadOnlyList<ExprCaseWhenThen> cases, ExprValue defaultValue)
        {
            this.Cases = cases;
            this.DefaultValue = defaultValue;
        }

        public IReadOnlyList<ExprCaseWhenThen> Cases { get; }

        public ExprValue DefaultValue { get; }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprCase(this);
    }

    public class ExprCaseWhenThen : ExprValue
    {
        public ExprCaseWhenThen(ExprBoolean condition, ExprValue value)
        {
            this.Condition = condition;
            this.Value = value;
        }

        public ExprBoolean Condition { get; }

        public ExprValue Value { get; }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprCaseWhenThen(this);
    }
}
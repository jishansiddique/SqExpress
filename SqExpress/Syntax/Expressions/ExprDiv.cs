﻿using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Expressions
{
    public class ExprDiv: ExprArithmetic
    {
        public ExprDiv(ExprValue left, ExprValue right)
        {
            this.Left = left;
            this.Right = right;
        }

        public ExprValue Left { get; }

        public ExprValue Right { get; }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprDiv(this);
    }
}
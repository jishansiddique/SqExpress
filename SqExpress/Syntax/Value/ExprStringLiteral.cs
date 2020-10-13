﻿namespace SqExpress.Syntax.Value
{
    public class ExprStringLiteral : ExprLiteral
    {
        public string? Value { get; }

        public ExprStringLiteral(string? value)
        {
            this.Value = value;
        }

        public static implicit operator ExprStringLiteral(string value)
            => new ExprStringLiteral(value);

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprStringLiteral(this);
    }
}
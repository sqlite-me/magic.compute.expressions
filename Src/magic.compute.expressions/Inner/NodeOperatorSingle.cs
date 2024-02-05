using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace magic.compute.expressions.Inner
{
    internal class NodeOperatorSingle : NodeOperator
    {
        public static readonly char[] opretorChars = { '!', '~'};
        public NodeOperatorSingle(string allExpressoin, int startIndex)
            : base(allExpressoin, startIndex)
        {
        }

        public NodeData Target { get; set; }
        public override bool NodeComplated => Target != null;
        public override Expression GetExpression()
        {
            Expression expression;
            switch (KeyWord)
            {
                case "!":
                    expression = Expression.IsFalse(Target.GetExpression());
                    break;
                case "~":
                    expression = Expression.Not(Target.GetExpression());
                    break;
                default:
                    throw new NotImplementedException(KeyWord);
            }
            return expression;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace MagicExpression.Inner
{
    internal class NodeArray : NodeData
    {
        public NodeArray(IEnumerable<NodeData> items, string allExpressoin, int startIndex, int endIndex = -1) : base(allExpressoin, startIndex, endIndex)
        {
            Items = items.ToArray();
            ExpClosed = true;
        }

        public override bool NodeComplated => true;

        public override Expression GetExpression()
        {
            var expArr = Items.Select(x => x.GetExpression()).ToArray();
            var maxType = expArr[0].Type;
            var maxTypeCode=Type.GetTypeCode(maxType);
            foreach (var exp in expArr)
            { var code = Type.GetTypeCode(exp.Type);
                if (maxTypeCode < code) {
                    maxTypeCode = code;
                    maxType =exp.Type;
                }
            }

            for (var i = 0; i < expArr.Length; i++)
            {
                var one = expArr[i];
                TypeCode tc = Type.GetTypeCode(one.Type);
                if (tc != maxTypeCode)
                {
                    expArr[i] = Expression.Convert(one, maxType);
                }
            }
            if (expArr.Length > 0)
            {
               return Expression.NewArrayInit(maxType, expArr);
            }

            throw new ExceptionErrorException(StartIndex, OrginalStr);
        }
        public IReadOnlyList<NodeData> Items { get; }
    }
}

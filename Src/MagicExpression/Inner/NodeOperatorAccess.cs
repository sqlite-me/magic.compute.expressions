using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace MagicExpression.Inner
{
    internal class NodeOperatorAccess : NodeOperator
    {
        public NodeOperatorAccess(string allExpressoin, int startIndex,int endIndex=-1)
            : base(allExpressoin, startIndex, endIndex)
        {
        }

        public NodeData Target { get; set; }

        public NodeData[] Params { get; set; }

        public override bool NodeComplated => Target != null && KeyWord?.Length > 0;

        public override Expression GetExpression()
        {
            switch (KeyWord)
            {
                case "[]":
                    var target = Target.GetExpression();
                    var @params =Params.Select(t=>t.GetExpression()).ToArray();
                    if (target.Type.IsArray)
                    {
                        return Expression.ArrayAccess(target, @params);
                    }
                    var pros = target.Type.GetProperties().Where(t => t.CanRead&&!t.GetMethod.ContainsGenericParameters);
                    foreach(var one in pros)
                    {
                        var paramInfos= one.GetIndexParameters();
                        if(paramInfos.Length== Params.Length)
                        {
                            return Expression.Property(target, one, @params);
                        }
                    }
                    throw new ExpressionErrorException(StartIndex, OrginalStr);
                default:
                    return Expression.PropertyOrField(Target.GetExpression(), KeyWord);
            }
        }
    }

}

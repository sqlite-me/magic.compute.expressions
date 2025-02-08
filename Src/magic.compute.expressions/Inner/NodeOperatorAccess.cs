using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace magic.compute.expressions.Inner
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
                    
                    var properties = target.Type.GetProperties().Where(t => t.CanRead&&!t.GetMethod.ContainsGenericParameters);
                    PropertyInfo? targetProperty = null;
                    PropertyInfo? failBackProperty = null;
                    ParameterInfo[]? tmpParameters = null;
                    foreach(var one in properties)
                    {
                        var paramInfos= one.GetIndexParameters();
                        if (paramInfos.Length == @params.Length)
                        {
                            failBackProperty ??= one;
                            bool allTypeOk = true;
                            bool anyIsSub = false;
                            for (int i = 0; i < paramInfos.Length; i++)
                            {
                                if (!paramInfos[i].ParameterType.IsAssignableFrom(@params[i].Type))
                                {
                                    allTypeOk = false;
                                    break;
                                }

                                if (tmpParameters != null)
                                {
                                    anyIsSub = anyIsSub || tmpParameters[i].ParameterType.IsAssignableFrom(paramInfos[i].ParameterType);
                                }
                            }
                            if (allTypeOk)
                            {
                                if(targetProperty == null|| anyIsSub)
                                {
                                    targetProperty = one;
                                    tmpParameters = paramInfos;
                                }
                            }
                        }
                    }
                    targetProperty ??= failBackProperty;
                    if (targetProperty != null)
                    {
                        return Expression.Property(target, targetProperty, @params);
                    }
                    throw new ExpressionErrorException(StartIndex, OrginalStr);
                default:
                    return Expression.PropertyOrField(Target.GetExpression(), KeyWord);
            }
        }
    }

}

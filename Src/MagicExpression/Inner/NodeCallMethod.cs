using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MagicExpression.Inner
{
    internal class NodeCallMethod : NodeOperator
    {
        private static Regex regex = new Regex(@"\w+");
        public NodeCallMethod(string orginalStr, int orignalIndex) :base(NodeType.CallMethod, regex.Match(orginalStr).Value, orginalStr, orignalIndex)
        {
        }

        public NodeData Target { get; set; }

        private Expression t()
        {
            Expression expression;
            switch (Target)
            {
                case NodeBracket nodeBracket:
                    if (nodeBracket.NodeDatas.Count == 1)
                    {
                        expression = nodeBracket.NodeDatas[0].GetExpression();
                    }
                    else
                    {
                        Type maxType = null;
                        TypeCode maxTypeCode = TypeCode.Empty;
                        var expArray = new Expression[nodeBracket.NodeDatas.Count];
                        for (var i = 0; i < expArray.Length; i++)
                        {
                            var one = nodeBracket.NodeDatas[i].GetExpression();
                            var typeCode = Type.GetTypeCode(one.Type);
                            expArray[i] = one;
                            if (typeCode > maxTypeCode)
                            {
                                maxTypeCode = typeCode;
                                maxType = one.Type;
                            }
                        }

                        for (var i = 0; i < expArray.Length; i++)
                        {
                            if (Type.GetTypeCode(expArray[i].Type) < maxTypeCode)
                                expArray[i] = Expression.Convert(expArray[i], maxType);
                        }
                        var arrType = expArray[0].Type.MakeArrayType(1);
                        expression = Expression.NewArrayInit(expArray[0].Type, expArray);
                    }
                    break;
                default:
                    expression = Target.GetExpression();
                    break;
            }


            return expression;
        }

        public override Expression GetExpression()
        {
            var exp = t();
            var method= exp.Type.GetMethod(base.Operator,new Type[0]);
            if(method != null&&!method.IsStatic)
            {
                return Expression.Call(exp,method);
            }
            Type elementType;
            if (exp.Type.IsArray)
            {
                elementType = exp.Type.GetElementType();
            }
            else if (exp.Type.IsGenericType)
            {
                elementType = exp.Type.GenericTypeArguments[0];
            }
            else
            {
                throw new MethodAccessException("not found method " + base.Operator);
            }

            method = typeof(System.Linq.Enumerable).GetMethod(base.Operator, new Type[] { typeof(IEnumerable<>).MakeGenericType(elementType) });
            if (method == null)
            {
                var methods = typeof(System.Linq.Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(t => t.Name == base.Operator&&t.GetParameters()?.Length==1).ToArray();

                method = methods.FirstOrDefault(t=> elementType.IsSubclassOf(t.GetParameters()[0].ParameterType) || elementType == t.GetParameters()[0].ParameterType);
                if (method == null)
                {
                    method = methods.FirstOrDefault(t => t.ContainsGenericParameters);
                    method=method.MakeGenericMethod(elementType);
                }
            }
            if (method == null)
            {
                throw new MethodAccessException("not found method " + base.Operator);
            }
            //return Expression.Call(methodInfo,expressionBody);
            return Expression.Call(method,exp);
        }
    }
}

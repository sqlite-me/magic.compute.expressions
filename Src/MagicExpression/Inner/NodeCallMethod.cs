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
    internal class NodeCallMethod : NodeOperatorAccess
    {
        public NodeCallMethod(string allExpressoin, int startIndex, int endIndex=-1) :base(allExpressoin, startIndex, endIndex)
        {
        }

        public override bool NodeComplated => _nodeComplated;
        private bool _nodeComplated;
        public void SetNodeComplated() {

            if (Target == null) throw new Exception(nameof(Target) + " can not be null");
            if (string.IsNullOrWhiteSpace(KeyWord)) throw new Exception(nameof(KeyWord) + " can not be empty");

            _nodeComplated = true;
        }
        private Expression getTargetExp()
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

        private List<Expression> getParamExp() {
        List<Expression> expArray = new List<Expression>();
            if(Params?.Length > 0)
            {
                foreach (var param in Params)
                {
                    expArray.Add(param.GetExpression());
                }
            }
            return expArray;
        }

        public override Expression GetExpression()
        {
            var targetExp = getTargetExp();
            var paramExps = getParamExp();
            var paramTypes= paramExps.Select(t=>t.Type).ToArray();
            var method= targetExp.Type.GetMethod(base.KeyWord, paramTypes);
            if(method != null&&!method.IsStatic)
            {
                return Expression.Call(targetExp,method,paramExps.ToArray());
            }
            Type elementType;
            if (targetExp.Type.IsArray)
            {
                elementType = targetExp.Type.GetElementType();
            }
            else if (targetExp.Type.IsGenericType)
            {
                elementType = targetExp.Type.GenericTypeArguments[0];
            }
            else
            {
                throw new MethodAccessException("not found method " + base.KeyWord);
            }

            paramTypes = new Type[] { typeof(IEnumerable<>).MakeGenericType(elementType) }.Concat(paramTypes).ToArray();
            method = typeof(System.Linq.Enumerable).GetMethod(base.KeyWord, paramTypes);
            if (method == null)
            {
                var methods = typeof(System.Linq.Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(t => t.Name == base.KeyWord&&t.GetParameters()?.Length== paramTypes.Length).ToArray();

                method = methods.FirstOrDefault(t=> elementType.IsSubclassOf(t.GetParameters()[0].ParameterType) || elementType == t.GetParameters()[0].ParameterType);
                if (method == null)
                {
                    method = methods.FirstOrDefault(t => t.ContainsGenericParameters);
                    method=method.MakeGenericMethod(elementType);
                }
            }
            if (method == null)
            {
                throw new MethodAccessException("not found method " + base.KeyWord);
            }
            //return Expression.Call(methodInfo,expressionBody);
            paramExps.Insert(0, targetExp);
            return Expression.Call(method, paramExps);
        }
    }
}

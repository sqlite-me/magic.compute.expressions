using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace magic.compute.expressions.Inner
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

        static MethodInfo? getMethodByTypes(MethodInfo[] methods, Type[] paramTypes)
        {
            var arr = methods.Select(t => new { t, args = t.GetParameters() })
                .Where(t => t.args.Length == paramTypes.Length || (t.args.Length > paramTypes.Length && t.args.Skip(paramTypes.Length).All(p => p.HasDefaultValue)))
                .ToArray();

            var nEqMethods = arr.ToList();
            nEqMethods.Clear();

            foreach (var method in arr)
            {
                var args = method.args;
                bool isEq = true;
                bool canAcc = true;
                for(var i=0; i<paramTypes.Length; i++)
                {
                    if (isEq && paramTypes[i] != args[i].ParameterType)
                    {
                        isEq = false;
                    }
                    if (canAcc && (paramTypes[i] != args[i].ParameterType || !args[i].ParameterType.IsAssignableFrom(paramTypes[i])))
                    {
                        canAcc = false;
                        if (args[i].ParameterType.IsGenericType)
                        {
                            if (paramTypes[i].HasElementType)
                            {
                                canAcc = true;
                            }
                        }
                    }

                    if (isEq == false && canAcc == false) break;
                }
                if (isEq == false && canAcc == false) continue;
                if (isEq && args.Length==paramTypes.Length)
                {
                    return method.t;
                }

                if(isEq || canAcc)
                {
                    nEqMethods.Add(method);
                }
            }

            if (nEqMethods.Count > 0)
            {
                return nEqMethods.OrderBy(t => t.args.Length).First().t;
            }
            return null;
        }

        public override Expression GetExpression()
        {
            var targetExp = getTargetExp();
            var targetType = (targetExp?.Type ?? (Target is NodeOperatorType nodeType ? nodeType.Type : null))
                ?? throw new MethodAccessException($"unknown method {base.KeyWord} at {base.StartIndex}");

            var paramExps = getParamExp();
            var paramTypes= paramExps.Select(t=>t.Type).ToArray();
            var methods = targetType.GetMethods((targetExp==null?BindingFlags.Static:BindingFlags.Instance)|BindingFlags.Public);
            methods= methods.Where(t=>t.Name==base.KeyWord).ToArray();

            var method = getMethodByTypes(methods, paramTypes);
            if (method != null && !method.IsStatic)
            {// call instance method
                return Expression.Call(targetExp, method, paramExps.ToArray());
            }
            else if (method?.IsStatic == true && targetExp == null)
            {// call static method
                return Expression.Call(method, paramExps.ToArray());
            }

            // call extention method
            Type elementType;
            if (targetType.IsArray)
            {
                elementType = targetType.GetElementType();
            }
            else if (targetType.IsGenericType)
            {
                elementType = targetType.GenericTypeArguments[0];
            }
            else
            {
                throw new MethodAccessException($"{base.KeyWord} is not a method for {targetType.FullName}");
            }

            paramTypes = new Type[] { typeof(IEnumerable<>).MakeGenericType(elementType) }.Concat(paramTypes).ToArray();
            method = typeof(System.Linq.Enumerable).GetMethod(base.KeyWord, paramTypes);
            if (method == null)
            {
                methods = typeof(System.Linq.Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
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
                throw new MethodAccessException($"{base.KeyWord} is not a extension method for {targetType.FullName} in System.Linq");
            }
            
            //return Expression.Call(methodInfo,expressionBody);
            paramExps.Insert(0, targetExp!);
            return Expression.Call(method, paramExps);
        }
    }
}

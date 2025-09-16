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
        public NodeCallMethod(string allExpressoin, int startIndex, int endIndex = -1) : base(allExpressoin, startIndex, endIndex)
        {
        }

        public override bool NodeComplated => _nodeComplated;
        private bool _nodeComplated;
        public void SetNodeComplated()
        {

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

        private List<Expression> getParamExp()
        {
            List<Expression> expArray = new List<Expression>();
            if (Params?.Length > 0)
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
                for (var i = 0; i < paramTypes.Length; i++)
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
                if (isEq && args.Length == paramTypes.Length)
                {
                    return method.t;
                }

                if (isEq || canAcc)
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
            var paramTypes = paramExps.Select(t => t.Type).ToArray();
            //var methods = targetType.GetMethods((targetExp==null?BindingFlags.Static:BindingFlags.Instance)|BindingFlags.Public);
            //methods= methods.Where(t=>t.Name==base.KeyWord).ToArray();

            //var method = getMethodByTypes(methods, paramTypes);
            var method = helper.FindBestMatchMethod(targetType, base.KeyWord, paramTypes, targetExp == null);

            var paramExpsCall = paramExps.ToList();
            if (method != null)
            {
                var pars = method.GetParameters();
                if (pars.Length > paramTypes.Length)
                {
                    for (var i = paramTypes.Length; i < pars.Length; i++)
                    {
                        paramExpsCall.Add(Expression.Constant(pars[i].DefaultValue));
                    }
                }
            }

            if (method != null && !method.IsStatic)
            {// call instance method
                return Expression.Call(targetExp, method, paramExpsCall.ToArray());
            }
            else if (method?.IsStatic == true && targetExp == null)
            {// call static method
                return Expression.Call(method, paramExpsCall.ToArray());
            }

            // call extention method
            if (!targetType.IsGenericType &&!targetType.HasElementType)
            {
                throw new MethodAccessException($"{base.KeyWord} is not a method for {targetType.FullName}");
            }

            paramTypes = new Type[] { targetType }.Concat(paramTypes).ToArray();
            method = typeof(System.Linq.Enumerable).GetMethod(base.KeyWord, paramTypes);
            if (method == null)
            {
                //methods = typeof(System.Linq.Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                //    .Where(t => t.Name == base.KeyWord && t.GetParameters()?.Length == paramTypes.Length).ToArray();

                //method = methods.FirstOrDefault(t => elementType.IsSubclassOf(t.GetParameters()[0].ParameterType) || elementType == t.GetParameters()[0].ParameterType);
                //if (method == null)
                //{
                //    method = methods.FirstOrDefault(t => t.ContainsGenericParameters);
                //    method = method.MakeGenericMethod(elementType);
                //}
                method = helper.FindBestMatchMethod(typeof(System.Linq.Enumerable), base.KeyWord, paramTypes, true);
            }
            if (method == null)
            {
                throw new MethodAccessException($"{base.KeyWord} is not a extension method for {targetType.FullName} in System.Linq");
            }

            //return Expression.Call(methodInfo,expressionBody);
            paramExps.Insert(0, targetExp!);
            paramExpsCall = paramExps.ToList();
            if (method != null)
            {
                var pars = method.GetParameters();
                if (pars.Length > paramTypes.Length)
                {
                    for (var i = paramTypes.Length; i < pars.Length; i++)
                    {
                        paramExpsCall.Add(Expression.Constant(pars[i].DefaultValue));
                    }
                }
            }

            return Expression.Call(method, paramExpsCall);
        }
    }

    class helper
    {
        /// <summary>
        /// 根据参数类型查找最匹配的方法（支持泛型方法）
        /// </summary>
        /// <param name="type">包含方法的类型</param>
        /// <param name="methodName">方法名</param>
        /// <param name="parameterTypes">参数类型数组</param>
        /// <param name="isStatic">是否是静态方法</param>
        /// <returns>最匹配的方法信息，找不到则返回null</returns>
        public static MethodInfo? FindBestMatchMethod(Type type, string methodName, Type[] parameterTypes, bool isStatic = false)
        {
            // 1. 获取所有候选方法（包含泛型定义）
            var candidateMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name == methodName &&
                           m.IsStatic == isStatic)
                .Select(t => ((MethodInfo Method, ParameterInfo[] Parameters))(t, t.GetParameters()))
                .Where(t => t.Parameters.Length >= parameterTypes.Length)
                .ToList();

            if (!candidateMethods.Any())
                return null;

            // 2. 分离普通方法和泛型方法定义
            var normalMethods = candidateMethods.Where(m => !m.Method.IsGenericMethodDefinition)
                .OrderBy(t => t.Parameters.Length).ToList();
            var genericMethodDefinitions = candidateMethods.Where(m => m.Method.IsGenericMethodDefinition)
                .OrderBy(t => t.Parameters.Length).ToList();

            // 3. 先检查普通方法是否有完全匹配
            var bestNormalMethod = FindBestMatchNormalMethod(normalMethods, parameterTypes);
            if (bestNormalMethod != null)
                return bestNormalMethod;

            // 4. 检查泛型方法是否有匹配
            return FindBestMatchGenericMethod(genericMethodDefinitions, parameterTypes);
        }

        /// <summary>
        /// 从普通方法中查找最匹配的方法
        /// </summary>
        private static MethodInfo? FindBestMatchNormalMethod(List<(MethodInfo Method, ParameterInfo[] Parameters)> methods, Type[] parameterTypes)
        {
            MethodInfo? bestMatch = null;
            int bestScore = int.MinValue;

            foreach (var (method, parameters) in methods)
            {
                int score = CalculateParameterMatchScore(parameters, parameterTypes);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = method;
                }
                // 如果分数相同，优先选择更具体的类型（如子类优先于父类）
                else if (score == bestScore && score != int.MinValue)
                {
                    if (IsMoreSpecific(method, bestMatch, parameterTypes))
                    {
                        bestMatch = method;
                    }
                }
            }

            return bestScore != int.MinValue ? bestMatch : null;
        }

        /// <summary>
        /// 从泛型方法定义中查找最匹配的方法
        /// </summary>
        private static MethodInfo? FindBestMatchGenericMethod(List<(MethodInfo, ParameterInfo[])> genericDefinitions, Type[] parameterTypes)
        {
            foreach (var (genericDefinition, parameters) in genericDefinitions)
            {
                // 获取泛型参数
                var typeParameters = genericDefinition.GetGenericArguments();

                // 尝试推断泛型类型参数
                var typeArguments = InferGenericTypeArguments(parameters, parameterTypes, typeParameters);
                if (typeArguments == null)
                    continue;

                // 构造封闭泛型方法
                try
                {
                    var closedMethod = genericDefinition.MakeGenericMethod(typeArguments);
                    // 验证参数是否匹配
                    if (IsParametersMatch(closedMethod.GetParameters(), parameterTypes))
                    {
                        return closedMethod;
                    }
                }
                catch (ArgumentException)
                {
                    // 泛型参数构造失败，跳过
                    continue;
                }
            }

            return null;
        }

        /// <summary>
        /// 推断泛型方法的类型参数
        /// </summary>
        private static Type[]? InferGenericTypeArguments(ParameterInfo[] parameters, Type[] argumentTypes, Type[] typeParameters)
        {
            var typeMap = new Dictionary<Type, Type>();

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i >= argumentTypes.Length)
                {
                    if (parameters[i].HasDefaultValue == false)
                        return null;
                }

                var paramType = parameters[i].ParameterType;
                var argType = argumentTypes[i];

                // 递归处理泛型参数
                if (!MatchType(paramType, argType, typeParameters, typeMap))
                {
                    return null;
                }
            }

            // 构建类型参数数组
            return typeParameters.Select(tp => typeMap[tp]).ToArray();
        }

        /// <summary>
        /// 匹配类型并构建泛型参数映射
        /// </summary>
        private static bool MatchType(Type paramType, Type argType, Type[] typeParameters, Dictionary<Type, Type> typeMap, bool allowBaseType = true)
        {
            // 如果参数类型是泛型参数
            if (typeParameters.Contains(paramType))
            {
                if (typeMap.TryGetValue(paramType, out var mappedType))
                {
                    // 已映射，检查是否一致
                    if (allowBaseType)
                    {
                        if (argType.IsAssignableFrom(mappedType))
                        {
                            typeMap[paramType] = argType;
                            return true;
                        }
                        else
                        {
                            return mappedType.IsAssignableFrom(argType);
                        }
                    }
                    else
                    {
                        return argType == mappedType;
                    }
                }
                else
                {
                    // 首次映射
                    typeMap[paramType] = argType;
                    return true;
                }
            }

            // 如果是泛型或数组类型
            if (paramType.IsGenericType && paramType.FullName == null)
            {
                var subTypeParams = paramType.GetGenericArguments();
                var subArgTypes = argType.HasElementType ? new[] { argType.GetElementType() } : argType.GetGenericArguments();
                if (subArgTypes.Length != subTypeParams.Length)
                    return false;

                if (argType.IsGenericType)
                {
                    var paramGenericType = paramType.GetGenericTypeDefinition();
                    var argGenericType = argType.GetGenericTypeDefinition();

                    // 泛型定义必须匹配
                    if (paramGenericType != argGenericType)
                        return false;
                }

                int i = 0;
                List<Type> argTypesTmp = new List<Type>();
                for (; i < subTypeParams.Length; i++)
                {
                    var subTypeParam = subTypeParams[i];
                    var subArgType = subArgTypes[i];
                    // 如果参数类型是泛型参数
                    if (typeParameters.Contains(subTypeParam))
                    {
                        if (typeMap.TryGetValue(subTypeParam, out var mappedType))
                        {
                            // 已映射，检查是否一致
                            if (mappedType == subArgType)
                            {
                                argTypesTmp.Add(mappedType);
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            // 首次映射
                            typeMap[subTypeParam] = subArgType;
                            argTypesTmp.Add(subArgType);
                        }
                    }
                    else
                    {
                        if (!MatchType(subTypeParam, subArgType, typeParameters, typeMap, false))
                        {
                            break;
                        }
                    }
                }
                if (i == subTypeParams.Length && argTypesTmp.Count > 0)
                {
                    var genericType = paramType.GetGenericTypeDefinition().MakeGenericType(argTypesTmp.ToArray());
                    if (allowBaseType && genericType.IsAssignableFrom(argType))
                    {
                        return true;
                    }
                    else if (genericType.Equals(argType))
                    {
                        return true;
                    }
                }
            }

            // 非泛型类型直接比较
            return paramType.IsAssignableFrom(argType);
        }

        /// <summary>
        /// 计算参数匹配分数（用于普通方法排序）
        /// </summary>
        private static int CalculateParameterMatchScore(ParameterInfo[] parameters, Type[] argumentTypes)
        {
            int score = 0;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i >= argumentTypes.Length)
                {
                    if (parameters[i].HasDefaultValue)
                    {
                        score--;
                        continue;
                    }
                    else
                    {
                        return int.MinValue;
                    }
                }
                var paramType = parameters[i].ParameterType;
                var argType = argumentTypes[i];

                if (paramType == argType)
                {
                    // 完全匹配，分数最高
                    score += 3;
                }
                else if (paramType.IsGenericType && argType.IsGenericType && paramType.IsGenericParameter &&
                         paramType.GetGenericTypeDefinition() == argType.GetGenericTypeDefinition())
                {
                    // 泛型类型定义匹配
                    score += 2;
                }
                else if (paramType.IsAssignableFrom(argType))
                {
                    // 可赋值（如父类），分数较低
                    score += 1;
                }
                else
                {
                    // 不匹配
                    return int.MinValue;
                }
            }

            return score;
        }

        /// <summary>
        /// 判断方法是否比另一个方法更具体（用于分数相同时的优先级）
        /// </summary>
        private static bool IsMoreSpecific(MethodInfo method1, MethodInfo? method2, Type[] argumentTypes)
        {
            if (method2 == null)
                return true;

            var parameters1 = method1.GetParameters();
            var parameters2 = method2.GetParameters();

            for (int i = 0; i < parameters1.Length; i++)
            {
                var paramType1 = parameters1[i].ParameterType;
                var paramType2 = parameters2[i].ParameterType;
                var argType = argumentTypes[i];

                // 如果method1的参数类型是method2参数类型的子类，且更接近实际参数类型
                if (paramType2.IsAssignableFrom(paramType1) && paramType1.IsAssignableFrom(argType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 验证参数是否匹配
        /// </summary>
        private static bool IsParametersMatch(ParameterInfo[] parameters, Type[] argumentTypes)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (!parameters[i].ParameterType.IsAssignableFrom(argumentTypes[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

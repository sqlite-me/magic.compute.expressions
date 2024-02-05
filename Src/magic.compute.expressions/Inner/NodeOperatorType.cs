using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;
using System.Text;

namespace magic.compute.expressions.Inner
{
    internal class NodeOperatorType : NodeOperator
    {
        public static readonly string[] OperatorKeys = { "is", "as", "not","("};

        private ParameterExpression variableExpression;
        private TypeBinaryExpression typeIsExpresson;
        private Expression valueExpression;

        public NodeOperatorType(string allExpressoin, int startIndex, int endIndex=-1)
            : base(allExpressoin, startIndex, endIndex)
        {
        }
        public NodeData Target { get; set; }
        public Type Type => getType();
        public string VariableName { get; internal set; }
        public Assembly[] AssemblysMayUsed { get; internal set; }
        public string TypeName { get; internal set; }
        public int ArrayBracketCount { get; internal set; }
        public override bool NodeComplated => base.ExpClosed&&Target!=null;
        private Type getType()
        {
            var typeName = this.TypeName;
            typeName = typeName?.Trim(' ', ')', '(');
            if (string.IsNullOrEmpty(typeName) || typeName.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            else
            {
                var type = getType(typeName);
                if (type == null)
                {
                    throw new ApplicationException("not found type " + typeName);
                }
                return type;
            }
        }

        public override Expression GetExpression()
        {
            Expression expression;
            switch (KeyWord)
            {
                case "as":
                    expression = asType();
                    break;
                case "is":
                    expression = GetTypeIsExpression();
                    break;
                case "isnot":
                    expression = Expression.IsFalse(GetTypeIsExpression());
                    break;
                case "()":
                    var paraExp = Target.GetExpression();
                    try
                    {
                        expression = Expression.Convert(paraExp, Type);
                    }
                    catch
                    {
                        var method = getConvert(paraExp.Type,Type,out Type needConvert);
                        if (method == null) throw;
                        if (needConvert!=null)
                        {
                            paraExp = Expression.Convert(paraExp, needConvert);
                        }
                        expression = Expression.Call(method,paraExp);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
            return expression;
        }

        private MethodInfo getConvert(Type fromType, Type toType,out Type? paramConvertTo)
        {
            paramConvertTo = null;
            var methods=typeof(Convert).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.ReturnType == toType && m.GetParameters()?.Length == 1);
            var method = methods.FirstOrDefault(m => m.GetParameters()[0].ParameterType == fromType);
            if (method != null) return method;

            var type=
            paramConvertTo = typeof(object);
            method = methods.FirstOrDefault(m => m.GetParameters()[0].ParameterType == type);
            return method;
        }

        public Expression GetValueExpresson()
        {
            if (!string.IsNullOrEmpty(VariableName) && valueExpression == null)
            {
                valueExpression = asType();
            }
            return valueExpression;
        }
        public ParameterExpression GetVariableExpression()
        {
            if (variableExpression == null)
            {
                variableExpression = Expression.Variable(Type, VariableName);
            }
            return variableExpression;
        }
        public TypeBinaryExpression GetTypeIsExpression()
        {
            if (typeIsExpresson == null)
            {
                typeIsExpresson = Expression.TypeIs(Target.GetExpression(), Type);
            }
            return typeIsExpresson;
        }
        public ConditionalExpression GetAssain()
        {
            var ifExp = GetTypeIsExpression();
            var varExp = GetVariableExpression();
            var valExp = GetValueExpresson();
            Expression thenExp;
            try { thenExp = Expression.Assign(varExp, valExp); }
            catch
            {
                thenExp = Expression.Assign(varExp, Expression.Default(varExp.Type));
            }
            return Expression.IfThen(ifExp, thenExp);
        }
        public void ClearVariable()
        {
            valueExpression = null;
            typeIsExpresson = null;
        }

        private Expression asType()
        {
            if (Type.IsValueType)
            {
                var exp = Target.GetExpression();
                try
                {
                    return Expression.Convert(exp, Type);
                }
                catch
                {
                    return exp;
                }
            }
            else
            {
                return Expression.TypeAs(Target.GetExpression(), Type);
            }
        }
        private Type? getType(string typeName)
        {
            Type? type;
            switch (typeName.ToLower())
            {
                case "int":
                case "Int32":
                    type = typeof(int);
                    break;
                case "uint":
                case "UInt32":
                    type = typeof(uint);
                    break;
                case "long":
                case "Int64":
                    type = typeof(long);
                    break;
                case "ulong":
                case "UInt64":
                    type = typeof(ulong);
                    break;
                case "double":
                case "Double":
                    type = typeof(double);
                    break;
                case "float":
                case "Float":
                    type = typeof(float);
                    break;
                case "decimal":
                case "Decimal":
                    type = typeof(decimal);
                    break;
                case "string":
                case "String":
                    type = typeof(string);
                    break;
                case "byte":
                case "Byte":
                    type = typeof(byte);
                    break;
                case "sbyte":
                case "SByte":
                    type = typeof(sbyte);
                    break;
                case "short":
                case "Int16":
                    type = typeof(short);
                    break;
                case "ushort":
                case "UInt16":
                    type = typeof(ushort);
                    break;
                case "datetime":
                case "DateTime":
                    type = typeof(DateTime);
                    break;
                case "timespan":
                case "TimeSpan":
                    type = typeof(TimeSpan);
                    break;
                case "object":
                case "Object":
                    type = typeof(object);
                    break;
                default:
                    var arr = typeName.Split(";assembly=");
                    typeName = arr[0];
                    if (arr.Length == 2)
                    {
                        type = Assembly.Load(arr[1])?.GetType(typeName);
                    }
                    else if (typeName.Contains('.'))
                    {
                        type = Type.GetType(typeName);
                        if (type == null && AssemblysMayUsed?.Length > 0)
                        {
                            foreach (var one in AssemblysMayUsed)
                            {
                                type = one.GetType(typeName);
                                if (type != null) break;
                            }
                        }
                    }
                    else
                    {
                        type = typeof(object).Assembly.GetExportedTypes().FirstOrDefault(t => t.Name == typeName);
                        if (type == null && AssemblysMayUsed?.Length > 0)
                        {
                            foreach (var one in AssemblysMayUsed)
                            {
                                type = one.GetTypes().FirstOrDefault(t => t.Name == typeName);
                                if (type != null) break;
                            }
                        }
                    }
                    break;
            }
            return type;
        }
    }
}

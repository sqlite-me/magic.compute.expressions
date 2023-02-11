using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace MagicExpression.Inner
{
    internal class NodeOperatorDouble : NodeOperator
    {
        private static readonly MethodInfo methodToString = typeof(object).GetMethod(nameof(Object.ToString))!;
        private static readonly Dictionary<Type, MethodInfo> dicSBMethods = new Dictionary<Type, MethodInfo>();
        static NodeOperatorDouble()
        {
            foreach (var one in typeof(StringBuilder).GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (one.Name != nameof(StringBuilder.Append)) continue;
                var paras = one.GetParameters();
                if (paras.Length == 1)
                {
                    dicSBMethods[paras[0].ParameterType] = one;
                }
            }
        }
        public NodeOperatorDouble(string @operator, int orignalIndex)
            : base(NodeType.Operator_Double, @operator, @operator, orignalIndex)
        {
        }

        public static MethodInfo Method => methodToString;

        public NodeData Left { get; set; }
        public NodeData Right { get; set; }

        public override Expression GetExpression()
        {
            var left = Left.GetExpression();
            var right = Right.GetExpression();
            Expression expression;
            switch (Operator)
            {
                case "+":
                    if (left.Type == typeof(string) || right.Type == typeof(string))
                    {
                        if (!dicSBMethods.ContainsKey(left.Type))
                        {
                            left = Expression.Convert(left, typeof(object));
                        }
                        else if (!dicSBMethods.ContainsKey(right.Type))
                        {
                            right = Expression.Convert(right, typeof(object));
                        }
                        var var = Expression.Variable(typeof(StringBuilder));
                        var assign = Expression.Assign(var, Expression.Constant(new StringBuilder()));
                        var appendLeft = Expression.Call(var, dicSBMethods[left.Type], left);
                        var appendRight = Expression.Call(var, dicSBMethods[right.Type], right);
                        var toString = Expression.Call(var, methodToString);
                        expression = Expression.Block(new ParameterExpression[] { var }, assign, appendLeft, appendRight, toString);
                    }
                    else if (canCheck(checkAndConvertToMax(ref left, ref right)))
                        expression = Expression.AddChecked(left, right);
                    else
                        expression = Expression.Add(left, right);
                    break;
                case "-":
                    if (canCheck(checkAndConvertToMax(ref left, ref right)))
                        expression = Expression.SubtractChecked(left, right);
                    else
                        expression = Expression.Subtract(left, right);
                    break;
                case "*":
                    if (canCheck(checkAndConvertToMax(ref left, ref right)))
                        expression = Expression.MultiplyChecked(left, right);
                    else
                        expression = Expression.Multiply(left, right);
                    break;
                case "/":
                    checkAndConvertToMax(ref left, ref right);
                    expression = Expression.Divide(left, right);
                    break;
                case "%":
                    checkAndConvertToMax(ref left, ref right);
                    expression = Expression.Modulo(left, right);
                    break;
                case "==":
                    checkAndConvertToMax(ref left, ref right);
                    try
                    {
                        expression = Expression.Equal(left, right);
                    }
                    catch (System.InvalidOperationException)
                    {
                        expression = Expression.ReferenceEqual(left, right);
                    }
                    break;
                case "!=":
                    checkAndConvertToMax(ref left, ref right);
                    try
                    {
                        expression = Expression.NotEqual(left, right);
                    }
                    catch (System.InvalidOperationException)
                    {
                        expression = Expression.ReferenceNotEqual(left, right);
                    }
                    break;
                case ">=":
                    checkAndConvertToMax(ref left, ref right);
                    expression = Expression.GreaterThanOrEqual(left, right);
                    break;
                case "<=":
                    checkAndConvertToMax(ref left, ref right);
                    expression = Expression.LessThanOrEqual(left, right);
                    break;
                case ">":
                    checkAndConvertToMax(ref left, ref right);
                    expression = Expression.GreaterThan(left, right);
                    break;
                case "<":
                    checkAndConvertToMax(ref left, ref right);
                    expression = Expression.LessThan(left, right);
                    break;
                case "&&":
                    expression = Expression.AndAlso(left, right);
                    break;
                case "||":
                    expression = Expression.OrElse(left, right);
                    break;
                case "??":
                    if (left.Type.IsValueType)
                    {
                        expression = left;
                    }
                    else if (left.Type == right.Type)
                    {
                        expression = Expression.Condition(Expression.Equal(left, Expression.Constant(null)),
                             right, left);
                    }
                    else{
                        expression = 
                            Expression.Condition(Expression.Equal(left, Expression.Constant(null)),
                            Expression.Convert(right, typeof(object)), Expression.Convert(left, typeof(object)));
                    }
                    break;
                case "&":
                    checkAndConvertToMax(ref left, ref right);
                    expression = Expression.And(left, right);
                    break;
                case "|":
                    checkAndConvertToMax(ref left, ref right);
                    expression = Expression.Or(left, right);
                    break;
                case ">>":
                    expression = Expression.RightShift(left, right);
                    break;
                case "<<":
                    expression = Expression.LeftShift(left, right);
                    break;
                case "^"://XOR
                    checkAndConvertToMax(ref left, ref right);
                    expression = Expression.ExclusiveOr(left, right);
                    break;
                default:
                    throw new NotImplementedException();
            }
            return expression;
        }

        private bool canCheck(TypeCode typeCode)
        {
            return typeCode >= TypeCode.Char && typeCode <= TypeCode.Decimal;
        }

        private TypeCode checkAndConvertToMax(ref Expression left, ref Expression right)
        {
            var leftTypeCode = Type.GetTypeCode(left.Type);
            var rightTypeCode = Type.GetTypeCode(right.Type);
            if (leftTypeCode == rightTypeCode)
            {
                return leftTypeCode;
            }
            else if (leftTypeCode > rightTypeCode && leftTypeCode < TypeCode.DateTime)
            {
                right = Expression.Convert(right, left.Type);
            }
            else if (leftTypeCode < rightTypeCode && rightTypeCode < TypeCode.DateTime)
            {
                left = Expression.Convert(left, right.Type);
            }

            return leftTypeCode > rightTypeCode ? leftTypeCode : rightTypeCode;
        }
    }
}

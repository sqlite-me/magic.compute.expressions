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
        public NodeOperatorAccess(string allExpressoin, int startIndex, int endIndex = -1)
            : base(allExpressoin, startIndex, endIndex)
        {
        }

        public NodeData Target { get; set; }

        public List<NodeData> Params { get; set; }

        public override bool NodeComplated => Target != null && KeyWord?.Length > 0;

        public override Expression GetExpression()
        {
            switch (KeyWord)
            {
                case "[]":
                    var target = Target.GetExpression();
                    var @params = Params.Select(t => t.GetExpression()).ToArray();
                    if (target.Type.IsArray)
                    {
                        return Expression.ArrayAccess(target, @params);
                    }

                    var properties = target.Type.GetProperties().Where(t => t.CanRead && !t.GetMethod.ContainsGenericParameters);
                    PropertyInfo? targetProperty = null;
                    PropertyInfo? failBackProperty = null;
                    ParameterInfo[]? tmpParameters = null;
                    foreach (var one in properties)
                    {
                        var paramInfos = one.GetIndexParameters();
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
                                if (targetProperty == null || anyIsSub)
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
                    var targetExp = Target.GetExpression();
                    if (targetExp == null && Target is NodeOperatorType nodeType && nodeType.Target == null)
                    {
                        var type = getPropertyOrFieldOwnerType(nodeType.Type);
                        switch (type)
                        {
                            case FieldInfo fieldInfo:
                                return Expression.Field(null,fieldInfo);
                            case PropertyInfo fieldInfo:
                                return Expression.Property(null, fieldInfo);
                        }
                    }
                    return Expression.PropertyOrField(targetExp, KeyWord);
            }
        }

        MemberInfo getPropertyOrFieldOwnerType(Type firstType)
        {
            var att = BindingFlags.Static | BindingFlags.Public | BindingFlags.GetField | BindingFlags.GetProperty;
            var type = firstType;
            var member = type.GetMember(KeyWord, att);
            if (member?.Length == 1)
            {
                return member[0];
            }

            type = typeof(Math);
            member = type.GetMember(KeyWord, att);
            if (member?.Length == 1)
            {
                switch (member[0])
                {
                    case FieldInfo fi:
                        if (fi.FieldType == firstType) return member[0]; break;
                    case PropertyInfo fi:
                        if (fi.PropertyType == firstType) return member[0]; break;
                }
            }
            type = typeof(MathF);
            member = type.GetMember(KeyWord, att);
            if (member?.Length == 1)
            {
                switch (member[0])
                {
                    case FieldInfo fi:
                        if (fi.FieldType == firstType) return member[0]; break;
                    case PropertyInfo fi:
                        if (fi.PropertyType == firstType) return member[0]; break;
                }
            }

            type = typeof(Convert);
            member = type.GetMember(KeyWord, att);
            if (member?.Length == 1)
            {
                switch (member[0])
                {
                    case FieldInfo fi:
                        if (fi.FieldType == firstType) return member[0]; break;
                    case PropertyInfo fi:
                        if (fi.PropertyType == firstType) return member[0]; break;
                }
            }
            return null;
        }
    }

}

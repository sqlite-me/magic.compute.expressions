using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace MagicExpression.Inner
{
    internal class Param
    {
        public Param(int index)
        {
            Index = index;
        }

        public List<NodeParam> ParamNodes { get; internal set; } = new List<NodeParam>();
        public int Index { get; }
        public ParameterExpression Parameter { get; private set; }

        internal void SetParameter(Type? type)
        {
            Parameter = Expression.Parameter(type ?? typeof(object), $"_{Index}");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace MagicExpression.Inner
{
    internal class NodeParam : NodeData
    {
        public NodeParam(int index, Param param, string orginalStr, int orignalIndex) : base(NodeType.Param, orginalStr, orignalIndex)
        {
            ParamIndex = index;
            Param = param;
        }

        public int ParamIndex { get; }
        public Param Param { get; }

        public override System.Linq.Expressions.Expression GetExpression()
        {
            return Param.Parameter;
        }
    }
}

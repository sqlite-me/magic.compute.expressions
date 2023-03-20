using System;
using System.Collections.Generic;
using System.Text;

namespace MagicExpression.Inner
{
    internal class NodeParam : NodeData
    {
        public NodeParam(Param param, string allExpressoin, int startIndex, int endIndex) : base(allExpressoin, startIndex,endIndex)
        {
            Param = param;
        }
        public Param Param { get; }
        public override bool ExpClosed { get => true; set => throw new Exception("can not change in Param"); }
        public override bool NodeComplated { get => true;  }
        public override System.Linq.Expressions.Expression GetExpression()
        {
            return Param.Parameter;
        }
    }
}

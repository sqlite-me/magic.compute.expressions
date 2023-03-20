using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using MagicExpression.Inner;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace MagicExpression
{
    public class MExpression
    {

        private readonly NodeData _root;
        private readonly Param[] _usedParams;
        private readonly IReadOnlyList<NodeOperatorType> _hasVariableNodes;
        private readonly Dictionary<string, Delegate> dicExistsDelegate = new Dictionary<string, Delegate>();
        public IReadOnlyList<int> ArgsIndexs { get; }

        public MExpression(string expressionStr)
        {
            if (expressionStr == null)
            {
                throw new ArgumentNullException(nameof(expressionStr));
            }

            var helper = new ExpressonHelper(expressionStr);
            helper.Parse();
            _root = helper.Root;
            _usedParams= helper.UsedParams.OrderBy(t => t.Index).ToArray();
            _hasVariableNodes= helper.HasVariableNodes;
            ArgsIndexs= _usedParams.Select(t=>t.Index).ToArray();
        }

        public Delegate GetDelegate(IList<object> args, out object[] usedArgs)
        {
            var key = args?.Count > 0 ? string.Join(",", args.Select(t => t == null ? "null" : t.GetType().FullName)):"";
            if (dicExistsDelegate.ContainsKey(key))
            {
                if(_usedParams?.Length> 0)
                {
                    usedArgs= new object[_usedParams.Length];
                    for (var i=0;i< _usedParams.Length; i++)
                    {
                        usedArgs[i] = args[_usedParams[i].Index];
                    }
                }
                else
                {
                    usedArgs= new object[0];
                }
                return dicExistsDelegate[key];
            }

            if(_hasVariableNodes?.Count> 0)
            {
                foreach (var node in _hasVariableNodes)
                {
                    node.ClearVariable();
                }
            }

            var tempArgs = new List<object>();
            List<ParameterExpression> parameters = new List<ParameterExpression>();
            foreach (var one in _usedParams)
            {
                tempArgs.Add(args[one.Index]);
                var type = args[one.Index]?.GetType();
                one.SetParameter(type);
                parameters.Add(one.Parameter);
            }

            if (_hasVariableNodes?.Count > 0 && args?.Count > 0)
            {
                var temp = args.Where(t => t != null).ToArray();
                if (temp.Length > 0)
                {
                    var assemblysMayUsed = Type.GetTypeArray(temp).Select(t => t.Assembly).Distinct().ToArray();
                    foreach(var one in _hasVariableNodes)
                    {
                        one.AssemblysMayUsed = assemblysMayUsed;
                    }
                }
            }

            var express = _root.GetExpression();
            var body = express;
            if (_hasVariableNodes?.Count > 0)
            {
                List<ParameterExpression> variables = new List<ParameterExpression>();
                List<Expression> bodys = new List<Expression>();
                foreach (var one in _hasVariableNodes)
                {
                    variables.Add(one.GetVariableExpression());
                    bodys.Add(one.GetAssain());
                }
                bodys.Add(express);
                body = Expression.Block(variables, bodys);
            }
            var lambda = Expression.Lambda(body, parameters.ToArray());
            var @delegate = lambda.Compile();
            usedArgs = tempArgs.ToArray();
            dicExistsDelegate[key] = @delegate;
            return @delegate;
        }
    }
}
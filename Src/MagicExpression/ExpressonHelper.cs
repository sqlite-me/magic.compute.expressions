using MagicExpression.Inner;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MagicExpression
{
    internal class ExpressonHelper
    {
        /// <summary>
        /// string | digit(int|double|flot...) | other word [a-z|A-Z|_|0_9]
        /// </summary>
        private static readonly Regex s_regexWord = new Regex("((?<!\\\\)\".*?(?<!\\\\)\"|(?<!\\w)((\\d+\\.?\\d*)|(\\d*\\.\\d+))((d|f|m|ul|lu|l|u)?)(?!\\w)|\\b\\w+\\b)|('.')", RegexOptions.IgnoreCase);
        private static readonly Regex s_regexNum = new Regex(@"\d+");
        private readonly string _expressionStr;
        private readonly Dictionary<int, string> _dicRegexWords;

        /// <summary>
        /// bracket ()
        /// </summary>
        private readonly List<NodeData> _notComplateNodeStack = new List<NodeData>();

        public IReadOnlyList<Param> UsedParams => _paramList;
        private readonly List<Param> _paramList=new List<Param>();
        public IReadOnlyList<NodeOperatorType> HasVariableNodes => _mayVariableTypes;
        private readonly List<NodeOperatorType> _mayVariableTypes = new List<NodeOperatorType>();

        public NodeData Root { get=>_root; }
        private NodeData _root;

        public ExpressonHelper(string expressionStr)
        {
            this._expressionStr = expressionStr;
            var rlt = s_regexWord.Matches(expressionStr);

            if (rlt.Count == 0)
            {
                throw new ArgumentException("not support expression string", nameof(expressionStr));
            }
            _dicRegexWords = rlt.ToDictionary(t => t.Index, t => t.Value);
        }
        private T? tryGetNotComplateTopNode<T>(string keyWord)
            where T : NodeData
        {
            if (_notComplateNodeStack.Count > 0 && _notComplateNodeStack[_notComplateNodeStack.Count - 1] is T nt)
            {
                if (nt.KeyWord == keyWord)
                {
                    return nt;
                }
            }
            return null;
        }
        private char getChar(int index)
        {
            if (_expressionStr.Length > index)
            {
                return _expressionStr[index];
            }
            throw new ExpressionErrorException(index, _expressionStr[index - 1].ToString());
        }

        private NodeData[] tryRemove(int index, int count = -1)
        {
            if (count == -1) count = _notComplateNodeStack.Count - index;
            NodeData[] rlt = new NodeData[count];
            for (int i = 0; i < rlt.Length; i++)
            {
                rlt[i] = _notComplateNodeStack[index];
                _notComplateNodeStack.RemoveAt(index);
            }
            return rlt;
        }

        private NodeOperatorType getTypeNodeByVarName(string name)
        {
            return _mayVariableTypes.FindLast(t => t.VariableName == name);
        }

        public void Parse()
        {
            //Trace.WriteLine(_expressionStr);
            string word;
            for (var i = 0; i < _expressionStr.Length; i += word.Length)
            {
                if (_dicRegexWords.ContainsKey(i))
                {
                    word = processRegexWord(i);
                }
                else
                {
                    word = processSingleLetter(i);
                }
            }

            //Trace.WriteLine(_expressionStr);

            var root = _notComplateNodeStack[0];
            if(_notComplateNodeStack.Count>1)
            {
                var bracketNode = new NodeBracket(_expressionStr, 0)
                {
                    EndIndex=_expressionStr.Length-1,
                    KeyWord="()",
                };
                bracketNode.NodeDatas.AddRange(_notComplateNodeStack);
                bracketNode.SetExpClosed();
                if (bracketNode.NodeDatas.Count > 1)
                {
                    var node = bracketNode.NodeDatas[1];
                    throw new ExpressionErrorException(node.StartIndex, node.OrginalStr);
                }
                root = bracketNode.NodeDatas[0];
            }

            if(_mayVariableTypes.Count>0)
            {
                for(var i=_mayVariableTypes.Count-1; i>=0; i--)
                {
                    if (string.IsNullOrEmpty(_mayVariableTypes[i].VariableName))
                    {
                        _mayVariableTypes.RemoveAt(i);
                    }
                }
            }

            _root = root;
        }

        private string processSingleLetter(int i)
        {
            var ch = _expressionStr[i];
          var  word = ch.ToString();
            switch (ch)
            {
                case '(':
                case '<':
                case '{':
                case '[':
                    NodeData notClosed = null;
                    if (ch == '<')
                    {
                        var ch2 = getChar(i + 1);
                        if (NodeOperatorDouble.opretorChars.Contains(ch2))
                        {
                            word += ch2;
                            notClosed = new NodeOperatorDouble(_expressionStr, i, i + word.Length - 1);
                        }
                        else
                        {
                            if (_notComplateNodeStack.Count == 0) { throw new ExpressionErrorException(i, word); }
                            if (_notComplateNodeStack[_notComplateNodeStack.Count - 1] is NodeOperatorType nType && !nType.ExpClosed)
                            {
                                nType.EndIndex = i + word.Length - 1;
                                nType.TypeName += '<';
                                nType.ArrayBracketCount++;
                                break;
                            }
                            else
                            {
                                notClosed = new NodeOperatorDouble(_expressionStr, i, i + word.Length - 1);
                            }
                        }
                    }

                    if (notClosed == null)
                    {
                        notClosed = new NodeBracket(_expressionStr, i);
                    }
                    _notComplateNodeStack.Add(notClosed);
                    break;
                case ')':
                case ']':
                    {
                        if (ch == ')'&& _notComplateNodeStack.LastOrDefault() is NodeOperatorType tNode)
                        {
                            if (tNode.KeyWord == "(")
                            {
                                tNode.KeyWord += word;
                                tNode.EndIndex = i;
                                tNode.ExpClosed = true;
                                break;
                            }
                            else
                            {
                                if(!tNode.ExpClosed)
                                tNode.ExpClosed = true;
                                //if(!tNode.NodeComplated)
                                //tNode.SetNodeComplated();
                            }
                        }
                        var preCh = ch == ')' ? "(" : "[";
                        var bIndex = _notComplateNodeStack.FindLastIndex(t => (t is NodeBracket nb) && nb.KeyWord == preCh);
                        if (bIndex < 0) throw new ExpressionErrorException(i, word);
                        var bracket = (NodeBracket)_notComplateNodeStack[bIndex];
                        var org = _expressionStr.Substring(bracket.StartIndex, i - bracket.StartIndex) + word;
                        bracket.EndIndex = i + word.Length - 1;
                        bracket.KeyWord += word;
                        var bracketNodes = tryRemove(bIndex + 1);
                        if (bracketNodes?.Length > 0)
                        {
                            var endNode = bracketNodes[bracketNodes.Length - 1];
                            if (!endNode.ExpClosed) endNode.ExpClosed = true;
                            foreach (var c in bracketNodes)
                            {
                                if (!c.ExpClosed)
                                {
                                    c.ExpClosed=true;
                                }
                                bracket.AddNode(c);
                            }
                        }
                        if (bracket.KeyWord == "()"&& _notComplateNodeStack.Count>1&&
                            _notComplateNodeStack[_notComplateNodeStack.Count-2] is NodeOperatorAccess aNode)
                        {
                            NodeCallMethod callMethodNode;
                            _notComplateNodeStack[_notComplateNodeStack.Count-2]= callMethodNode=
                                new NodeCallMethod(_expressionStr, aNode.StartIndex, i + word.Length - 1) {
                                KeyWord = aNode.KeyWord,
                                Target=aNode.Target,
                                };
                            // process params
                            callMethodNode.Params= bracket.NodeDatas.ToArray();
                            _notComplateNodeStack.RemoveAt(_notComplateNodeStack.Count - 1);
                            callMethodNode.ExpClosed=true;
                            callMethodNode.SetNodeComplated();
                            bracket = null;
                        }
                        else if(bracket.KeyWord == "[]")
                        {
                            if (_notComplateNodeStack.Count > 1)
                            {
                                var node = _notComplateNodeStack[_notComplateNodeStack.Count - 2];
                                if (node.ExpClosed)
                                {
                                    switch (node)
                                    {
                                        case NodeBracket _:
                                        case NodeConst _:
                                        case NodeOperatorAccess _:
                                        case NodeParam _:
                                        case NodeVariable _:
                                            NodeOperatorAccess aNodeNew;
                                            _notComplateNodeStack[_notComplateNodeStack.Count - 2] = aNodeNew =
                                                new NodeCallMethod(_expressionStr, node.StartIndex, i + word.Length - 1)
                                                {
                                                    KeyWord = "[]",
                                                    Target = node,
                                                };
                                            bracket.SetExpClosed();
                                            aNodeNew.Params = bracket.NodeDatas.ToArray();
                                            _notComplateNodeStack.RemoveAt(_notComplateNodeStack.Count - 1);
                                            aNodeNew.ExpClosed = true;
                                            bracket = null;
                                            break;
                                    }
                                }
                            }

                            if (bracket != null)// array
                            {
                                _notComplateNodeStack[_notComplateNodeStack.Count - 1] =
                                    new NodeArray(bracket.NodeDatas, _expressionStr, bracket.StartIndex, bracket.EndIndex);
                                bracket=null;
                            }
                        }
                        if (bracket != null)
                        {
                            bracket.SetExpClosed();
                            if (bracket.NodeDatas.Count > 1)
                            {
                                var node = bracket.NodeDatas[1];
                                throw new ExpressionErrorException(node.StartIndex, node.OrginalStr);
                            }
                            _notComplateNodeStack[bIndex] = bracket.NodeDatas[0];
                        }
                    }
                    break;
                case ',':
                    if (_notComplateNodeStack.Count == 0||_expressionStr.Length==i+1) throw new ExpressionErrorException(i, word);// not in begin or end
                    switch (_notComplateNodeStack[_notComplateNodeStack.Count - 1])
                    {
                        case NodeOperatorType nType:
                            if (!nType.ExpClosed)
                            {
                                nType.TypeName += word;
                                nType.EndIndex = i + word.Length - 1;
                            }
                            break;// ignore others
                    }
                    break;
                case '>':
                    {
                        if (_notComplateNodeStack.LastOrDefault() is NodeOperatorType nType && !nType.ExpClosed)
                        {
                            nType.EndIndex = i + word.Length - 1;
                            nType.ArrayBracketCount--;
                            nType.TypeName += word;
                            break;
                        }
                        if (_expressionStr.Length > i + 1)
                        {
                            var ch2 = _expressionStr[i + 1];
                            if (NodeOperatorDouble.opretorChars.Contains(ch2))
                            {// double key word operator
                                word += ch2;
                            }
                        }
                        _notComplateNodeStack.Add(new NodeOperatorDouble(_expressionStr, i, i + word.Length - 1));
                    }
                    break;
                case '}':
                    {
                        var bIndex = _notComplateNodeStack.FindLastIndex(t => (t is NodeBracket nb) && nb.OrginalStr == "{");
                        if (_notComplateNodeStack.Count > 1 &&
                            _notComplateNodeStack[_notComplateNodeStack.Count - 2] is NodeBracket bracket &&
                             _notComplateNodeStack[_notComplateNodeStack.Count - 1] is NodeConst cnst && cnst.Value is int index)
                        {
                            var org = _expressionStr.Substring(bracket.StartIndex, i - bracket.StartIndex) + word;
                            var param = _paramList.Find(t => t.Index == index);
                            if (param == null)
                            {
                                _paramList.Add(param = new Param(index));
                            }
                            var nodeParam = new NodeParam(param, _expressionStr, bracket.StartIndex, i);
                            param.ParamNodes.Add(nodeParam);
                            _notComplateNodeStack[_notComplateNodeStack.Count - 2] = nodeParam;
                            tryRemove(_notComplateNodeStack.Count - 1);
                        }
                    }
                    break;
                case '!':
                    if (getChar(i + 1) == '=')
                    {
                        word += "=";
                        _notComplateNodeStack.Add(new NodeOperatorDouble(_expressionStr, i, i + 1));
                    }
                    else
                        _notComplateNodeStack.Add(new NodeOperatorSingle(_expressionStr, i));
                    break;
                case '~':
                    _notComplateNodeStack.Add(new NodeOperatorSingle(_expressionStr, i));
                    break;
                case '?':
                    if (getChar(i + 1) == '?')
                    {
                        word += "?";
                        _notComplateNodeStack.Add(new NodeOperatorDouble(_expressionStr, i, i + 1));
                    }
                    else
                        _notComplateNodeStack.Add(new NodeOperatorTernary(_expressionStr, i));
                    break;
                case ':':
                    {
                        var tIndex = _notComplateNodeStack.FindLastIndex(t => t is NodeOperatorTernary ot && ot.KeyWord == "?");
                        if (tIndex >= 0 && tIndex < _notComplateNodeStack.Count - 1)
                        {
                            var ter = (NodeOperatorTernary)_notComplateNodeStack[tIndex];
                            ter.EndIndex = i + word.Length - 1;
                            ter.KeyWord += word;
                            var rlt = tryRemove(tIndex + 1);
                            if (rlt.Length == 1)
                            {
                                ter.Second = rlt[0];
                            }
                            else
                            {
                                var bareck = new NodeBracket(_expressionStr, ter.StartIndex);
                                foreach (var item in rlt)
                                {
                                    bareck.AddNode(item);
                                }
                                bareck.SetExpClosed();
                                ter.Second = bareck.NodeDatas[0];
                            }
                        }
                    }
                    break;
                case '.':
                    if (_notComplateNodeStack.Count == 0 || _expressionStr.Length - 1 == i)
                        throw new ExpressionErrorException(i, word);// dot not at first or end, (.1 or 1.0 matched by Regex)

                    var preNode = _notComplateNodeStack[_notComplateNodeStack.Count - 1];
                    if (preNode is NodeOperatorType nType4Dot && !nType4Dot.ExpClosed)
                    {
                        nType4Dot.EndIndex = i + word.Length - 1;
                        nType4Dot.TypeName += word;
                    }
                    else
                    {
                        _notComplateNodeStack.RemoveAt(_notComplateNodeStack.Count - 1);
                        _notComplateNodeStack.Add(new NodeOperatorAccess(_expressionStr, i) {Target=preNode });
                    }
                    break;
                default:
                    if (string.IsNullOrWhiteSpace(word)) break;
                    else if (NodeOperatorDouble.opretorChars.Contains(ch))
                    {
                        var ch2 = getChar(i + 1);
                        if (NodeOperatorDouble.opretorChars.Contains(ch2))
                        {
                            word += ch2;
                        }
                        _notComplateNodeStack.Add(new NodeOperatorDouble(_expressionStr, i, i + word.Length - 1));
                    }
                    else
                    {
                        throw new ExpressionErrorException(i, word);
                    }
                    break;
            }
            return word;
        }

        private string processRegexWord(int i)
        {
            string word = _dicRegexWords[i];
            var unkown = false;
            switch (word)
            {
                case "true":
                    _notComplateNodeStack.Add(new NodeConst(true, _expressionStr, i, i + 3));
                    break;
                case "false":
                    _notComplateNodeStack.Add(new NodeConst(false, _expressionStr, i, i + 4));
                    break;
                case "null":
                    _notComplateNodeStack.Add(new NodeConst(null, _expressionStr, i, i + 3));
                    break;
                case "is":
                case "as":
                    _mayVariableTypes.Add(new NodeOperatorType(_expressionStr, i, i + 1));
                    _notComplateNodeStack.Add(_mayVariableTypes[_mayVariableTypes.Count - 1]);
                    break;
                case "not":
                    {
                        var nt = tryGetNotComplateTopNode<NodeOperatorType>("is");
                        if (nt != null)
                        {
                            nt.EndIndex = i + word.Length - 1;
                            nt.KeyWord += word;
                        }
                        else unkown = true;
                    }
                    break;
                default:
                    if (word[0] == '"')// string
                    {
                        _notComplateNodeStack.Add(new NodeConst(word.Trim('"'), _expressionStr, i, i + word.Length - 1)); break;
                    }
                    else if ((word.Length == 3 && word[0] == '\'' && word[2] == '\''))// char
                    {
                        _notComplateNodeStack.Add(new NodeConst(word[1], _expressionStr, i, i + word.Length - 1));
                        break;
                    }
                    else
                    {
                        var numMatch = s_regexNum.Match(word);
                        if (numMatch.Success && (numMatch.Index == 0 || (numMatch.Index == 1 && word[0] == '.')))
                        {
                            int orgI = i;
                            string org = word;
                            object? value = null;
                            if (_notComplateNodeStack.Count > 0 &&
                                _notComplateNodeStack[_notComplateNodeStack.Count - 1] is NodeOperatorDouble on && on.KeyWord == "-")
                            {
                                var pre = _notComplateNodeStack?.Count > 1 ? _notComplateNodeStack[_notComplateNodeStack.Count - 2] : null;
                                var negative = false;
                                switch (pre)
                                {
                                    case null:
                                    case NodeOperatorDouble _:
                                    case NodeOperatorTernary _:
                                        negative = true;
                                        break;
                                    case NodeBracket bracket:
                                        negative = bracket.KeyWord.Length == 1;
                                        break;
                                }
                                if (negative)
                                {
                                    value = stringToNumConst("-" + word);
                                    org = _expressionStr.Substring(orgI = on.StartIndex, i - on.StartIndex) + word;
                                    _notComplateNodeStack.RemoveAt(_notComplateNodeStack.Count - 1);
                                }
                            }
                            if (value == null)
                            {
                                value = stringToNumConst(org);
                            }
                            _notComplateNodeStack.Add(new NodeConst(value, _expressionStr, orgI, orgI + org.Length - 1));
                            break;
                        }
                    }
                    unkown = true;
                    break;
            }
            if (unkown)
            {
                if (_notComplateNodeStack.Count == 0) throw new ExpressionErrorException(i, word);

                var preNode = _notComplateNodeStack[_notComplateNodeStack.Count - 1];
                switch (preNode)
                {
                    case NodeBracket bracket:
                        if (bracket.ExpClosed) throw new ExpressionErrorException(i, word);
                        var typeNode = getTypeNodeByVarName(word);
                        if (typeNode == null)
                        {
                            _notComplateNodeStack[_notComplateNodeStack.Count - 1] =// type convert
                                new NodeOperatorType(_expressionStr, bracket.StartIndex,i+word.Length-1) {TypeName=word ,KeyWord= bracket.KeyWord };
                        }
                        else
                        {
                            _notComplateNodeStack.Add(new NodeVariable(typeNode, _expressionStr, i, i + word.Length - 1));// variable
                        }
                        break;
                    case NodeOperatorType nodeType:
                        if (nodeType.ExpClosed) throw new ExpressionErrorException(i, word);
                        if (nodeType.KeyWord!="()") // is is not as 
                        {
                            if (nodeType.ArrayBracketCount > 0 ||
                                string.IsNullOrEmpty(nodeType.TypeName) ||
                                nodeType.TypeName.EndsWith('.'))//
                            {
                                nodeType.TypeName += word;// type name 
                                nodeType.EndIndex = i + word.Length - 1;
                            }
                            else
                            {
                                nodeType.VariableName = word;
                                nodeType.ExpClosed = true;
                            }
                        }
                        else
                        {
                            nodeType.TypeName += word;// type name 
                            nodeType.EndIndex = i + word.Length - 1;
                        }
                        break;
                    case NodeOperatorAccess nodeAccess:
                        if (nodeAccess.OrginalStr == ".") // access
                        {
                            nodeAccess.KeyWord = word;
                            nodeAccess.EndIndex = i + word.Length - 1;
                        }
                        else throw new ExpressionErrorException(i, word);
                        break;
                    default:
                        var varTypeNode = _mayVariableTypes.FindLast(t=>t.VariableName==word);
                        if(varTypeNode==null) throw new ExpressionErrorException(i,word);
                        _notComplateNodeStack.Add(new NodeVariable(varTypeNode,_expressionStr,i,i+word.Length-1));
                        break;
                }
                //_notComplateNodeStack.Add(new NodeUnkonw(_expressionStr, i,i+word.Length-1));
            }

            return word;
        }

        private object stringToNumConst(string value)
        {
            char? last = value.Length > 0 ? (char?)value[^1] : null!;
            char? lastSecond = value.Length > 1 ? (char?)value[^2] : null;
            string type;
            string val;
            if (lastSecond.HasValue && ((lastSecond > 'a' && lastSecond < 'z') || (lastSecond > 'A' && lastSecond < 'Z')))
            {
                val = value[..^2];
                type = new string(new[] { lastSecond.Value, last.Value });
            }
            else if (last.HasValue && ((last > 'a' && last < 'z') || (last > 'A' && last < 'Z')))
            {
                val = value[..^1];
                type = new string(new[] { last.Value });
            }
            else if (value.Contains('.'))
            {
                val = value;
                type = "D";
            }
            else
            {
                val = value;
                type = "";
            }
            switch (type)
            {
                case "D":
                case "d":
                    return double.Parse(val);
                case "F":
                case "f":
                    return float.Parse(val);
                case "M":
                case "m":
                    return decimal.Parse(val);
                case "UL":
                case "LU":
                case "ul":
                case "lu":
                case "Ul":
                case "Lu":
                case "uL":
                case "lU":
                    return ulong.Parse(val);
                case "L":
                case "l":
                    return long.Parse(val);
                case "U":
                case "u":
                    return uint.Parse(val);
                default:
                    return int.Parse(val);
            }
        }

    }
}

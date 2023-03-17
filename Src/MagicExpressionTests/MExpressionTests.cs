﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using MagicExpression;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq.Expressions;

namespace MagicExpression.Tests
{
    [TestClass()]
    public class MExpressionTests
    {
        private object call(string exp, object[] args)
        {
            var @delegate = new MExpression(exp).GetDelegate(args,out object[] usedArgs);
            var result = @delegate.DynamicInvoke(usedArgs);
            return result;
        }
        [TestMethod()]
        public void GetDelegateTest()
        {
            var expStr = "{0} is not System.Int32";
            var args = new object[] { 1 };
            var rlt = call(expStr, args);
            Assert.AreEqual(rlt, false);

            
            expStr = "({0} ?? false) ? \"yes\" : \"no\"";
            args = new object[] { null };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, "no");

            expStr = "(({0} == null) || ({0} == \"\"))";
            args = new object[] { "1, 6" };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, false);

            expStr = "(({0} == null) || ({0} == \"\")) ? \"/Image/empty_big.png\" : {0}";
            args = new object[] { "1, 6" };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, "1, 6");


            expStr = "((({0} == null)) || ({0} == \"\")) ? (\"/Image/empty_big.png\") : {0}";
            args = new object[] { "1, 6" };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, "1, 6");

            expStr = "((({0} == null)) || ({0} == \"\")) ? (\"/Image/empty_big.png\") : {0}";
            args = new object[] { null };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, "/Image/empty_big.png");


            expStr = "{0} +2 * {1}";
            args = new object[] { 1, 6 };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, 13);

            expStr = "({0} +{2}) * ({1} -{3})";
            args = new object[] { 1, 6 ,3,10};
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, -16);

            expStr = "({0} >=2)? 2 : {1}";
            args = new object[] { 1, 6 };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, 6);


            expStr = "({1}-{0}).TotalMinutes + {2}";
            DateTime t1 = DateTime.Now, t2 = DateTime.Now.AddMinutes(5);
            args = new object[] { t1, t2, 10 };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, (t2 - t1).TotalMinutes + 10);


            expStr = "({1}??1.0) / (double){0}";
            args = new object[] { 2, null };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, .5);

            args = new object[] { 2, 19 };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, 9.5);

            expStr = "({1}??1.0) >> {0}";
            args = new object[] { 2, 4 };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, 1);

            expStr = "({1}??1.0) >> {0}";
            args = new object[] { 2, 4L };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, 1L);

            expStr = "{0} + {1}";
            args = new object[] { "12345", 4L };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, "12345" + 4L);

            DateTime? time = DateTime.Now;
            expStr = "({0} is DateTime)? ({0} as DateTime).Year: {1}";
            args = new object[] { (object)time, "i an string" };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, 2023);

            expStr = "({0} is DateTime)? ({0} as TimeSpan).Days: {1}";
            args = new object[] { (object)(DateTime.Now - DateTime.MinValue), "i an string" };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, "i an string");


            expStr = "({0} is TimeSpan ts)? ts.Days: {1}";
            args = new object[] { (object)(DateTime.Now - DateTime.MinValue), "i an string" };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, (DateTime.Now - DateTime.MinValue).Days);

            expStr = "({0} is not TimeSpan ts)? {1}:ts.Days";
            args = new object[] { (object)(DateTime.Now - DateTime.MinValue), "i an string" };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, (DateTime.Now - DateTime.MinValue).Days);

            expStr = "({0} is IList ts)? \"yes\": \"no\"";
            args = new object[] { (object)(new string[] {"a","b","c" }), "yes","no" };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, "yes");


            expStr = "({0} is MagicExpression.MExpression ts)? {1}: {2}"; 
            args = new object[] { (object)(new string[] { "a", "b", "c" }), "yes", "no" };
            rlt = call(expStr, args);
            Assert.AreEqual(rlt, "no");

            var mExp = new MExpression("{0} + {1} * {2} /{3}");
            var @delegate = mExp.GetDelegate(new object[] {12,3,4f,2 }, out args);
            rlt = @delegate.DynamicInvoke(args);
            Assert.AreEqual(rlt, 18f);

            @delegate = mExp.GetDelegate(new object[] { 20, 3, 4d, 2 }, out args);
            rlt = @delegate.DynamicInvoke(args);
            Assert.AreEqual(rlt, 26d);


            mExp = new MExpression("{0} + {0} * {2} /{3}");
             @delegate = mExp.GetDelegate(new object[] { 12, 3, 4f, 2 }, out args);
            rlt = @delegate.DynamicInvoke(args);
            Assert.AreEqual(rlt, 36f);

            @delegate = mExp.GetDelegate(new object[] { 20, 3, 4d, 2 }, out args);
            rlt = @delegate.DynamicInvoke(args);
            Assert.AreEqual(rlt, 60d);

            mExp = new MExpression("({0},{0},{2},{3}).Sum()");
            @delegate = mExp.GetDelegate(new object[] { 20, 3, 4d, 2 }, out args);
            rlt = @delegate.DynamicInvoke(args);
            Assert.AreEqual(rlt, 46d);

            mExp = new MExpression("({0},{0},{2},{3}).Max()");
            @delegate = mExp.GetDelegate(new object[] { 20, 3, 4d, 2 }, out args);
            rlt = @delegate.DynamicInvoke(args);
            Assert.AreEqual(rlt, 20d);

            mExp = new MExpression("({0},{0},{2},{3}).Min()");
            @delegate = mExp.GetDelegate(new object[] { 20, 3, 4d, 2 }, out args);
            rlt = @delegate.DynamicInvoke(args);
            Assert.AreEqual(rlt, 2d);

            var timeNow = DateTime.Now;
            var timeDate = timeNow.Date;
            var max = new[] { timeNow, timeDate }.Max();
            mExp = new MExpression("({0},{1}).Max()");
            @delegate = mExp.GetDelegate(new object[] { timeNow, timeDate }, out args);
            rlt = @delegate.DynamicInvoke(args);
            Assert.AreEqual(rlt, timeNow);

            mExp = new MExpression("({0},{1}).Min()");
            @delegate = mExp.GetDelegate(new object[] { timeNow, timeDate }, out args);
            rlt = @delegate.DynamicInvoke(args);
            Assert.AreEqual(rlt, timeDate);


            mExp = new MExpression("({0} is not DateTime t1)?{1}:(({1} is not DateTime t2)?t1:(t1,t2).Max())");
            @delegate = mExp.GetDelegate(new object[] {new TimeSpan(), timeDate }, out args);
            rlt = @delegate.DynamicInvoke(args);
            Assert.AreEqual(rlt, timeDate);

            mExp = new MExpression("({0} is not DateTime t1||{1} is not DateTime t2)?null:(t1,t2).Max()");
            @delegate = mExp.GetDelegate(new object[] { new TimeSpan(), timeDate }, out args);
            rlt = @delegate.DynamicInvoke(args);
            Assert.AreEqual(rlt, null);

            mExp = new MExpression("(({0} is not DateTime t1) ||( {1} is not DateTime t2))?null:(t1,t2).Max()");
            @delegate = mExp.GetDelegate(new object[] { timeNow, timeDate }, out args);
            rlt = @delegate.DynamicInvoke(args);
            Assert.AreEqual(rlt, timeNow);

            var exp = "(({0} is DateTime preEnd)&&({1} is DateTime preEsEnd)&&({2} is DateTime start)&&({3} is double scale))?" +
                "(" +
                "((start-(preEnd,preEsEnd).Max()).TotalSeconds is double timeDiff)?(timeDiff * scale):0d" +
                ")" +
                ":0d";

            mExp = new MExpression(exp);
            @delegate = mExp.GetDelegate(new object[] { timeNow, timeNow.AddSeconds(1), timeNow.AddSeconds(2), 0.1d }, out args);
            rlt = @delegate.DynamicInvoke(args);
            Assert.AreEqual(rlt, 0.2);

            var exp = "(({0} is DateTime preEnd)&&({1} is DateTime preEsEnd)&&({2} is DateTime start)&&({3} is double scale))?" +
                "(" +
                "((start-(preEnd,preEsEnd).Max()).TotalSeconds is double timeDiff)?(timeDiff * scale):0d" +
                ")" +
                ":0d";

            mExp = new MExpression(exp);
            @delegate = mExp.GetDelegate(new object[] { timeNow, timeNow.AddSeconds(1), timeNow.AddSeconds(2),0.1d }, out args);
            rlt = @delegate.DynamicInvoke(args);
            Assert.AreEqual(rlt, 0.2);
        }
    }
}
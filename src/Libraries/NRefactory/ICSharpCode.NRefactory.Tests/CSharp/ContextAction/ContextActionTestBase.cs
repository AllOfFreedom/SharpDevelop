﻿// 
// ContextActionTestBase.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Threading;

namespace ICSharpCode.NRefactory.CSharp.ContextActions
{
	public abstract class ContextActionTestBase
	{
		protected static string RunContextAction (IContextAction action, string input)
		{
			var context = TestRefactoringContext.Create (input);
			bool isValid = action.IsValid (context);
			if (!isValid)
				Console.WriteLine ("invalid node is:" + context.GetNode ());
			Assert.IsTrue (isValid, action.GetType () + " is invalid.");
			
			action.Run (context);
			
			return context.doc.Text;
		}
		
		protected static void TestWrongContext (IContextAction action, string input)
		{
			var context = TestRefactoringContext.Create (input);
			bool isValid = action.IsValid (context);
			if (!isValid)
				Console.WriteLine ("invalid node is:" + context.GetNode ());
			Assert.IsTrue (!isValid, action.GetType () + " shouldn't be valid there.");
		}
	}
}
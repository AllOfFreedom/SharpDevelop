﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.SharpDevelop.Project;

namespace ICSharpCode.UnitTesting
{
	public interface IRegisteredTestFrameworks
	{
		ITestFramework GetTestFrameworkForProject(IProject project);
		ITestRunner CreateTestRunner(IProject project);
		ITestRunner CreateTestDebugger(IProject project);
		
		bool IsTestMember(IUnresolvedMember member);
		bool IsTestClass(IUnresolvedTypeDefinition typeDefinition);
		bool IsTestProject(IProject project);
		
		IEnumerable<IUnresolvedMember> GetTestMembersFor(IUnresolvedTypeDefinition typeDefinition);

		bool IsBuildNeededBeforeTestRunForProject(IProject project);		
	}
}
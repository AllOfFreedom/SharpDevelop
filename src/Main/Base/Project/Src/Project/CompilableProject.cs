﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.IO;
using System.ComponentModel;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Debugging;
using System.Diagnostics;
using System.Collections.Generic;
using MSBuild = Microsoft.Build.BuildEngine;

namespace ICSharpCode.SharpDevelop.Project
{
	public enum OutputType {
		[Description("${res:Dialog.Options.PrjOptions.Configuration.CompileTarget.Exe}")]
		Exe,
		[Description("${res:Dialog.Options.PrjOptions.Configuration.CompileTarget.WinExe}")]
		WinExe,
		[Description("${res:Dialog.Options.PrjOptions.Configuration.CompileTarget.Library}")]
		Library,
		[Description("${res:Dialog.Options.PrjOptions.Configuration.CompileTarget.Module}")]
		Module
	}
	
	/// <summary>
	/// A compilable project based on MSBuild.
	/// </summary>
	public abstract class CompilableProject : MSBuildBasedProject
	{
		#region Static methods
		/// <summary>
		/// Gets the file extension of the assembly created when building a project
		/// with the specified output type.
		/// Example: OutputType.Exe => ".exe"
		/// </summary>
		public static string GetExtension(OutputType outputType)
		{
			switch (outputType) {
				case OutputType.WinExe:
				case OutputType.Exe:
					return ".exe";
				case OutputType.Module:
					return ".netmodule";
				default:
					return ".dll";
			}
		}
		#endregion
		
		/// <summary>
		/// A list of project properties that cause reparsing of references when they are changed.
		/// </summary>
		protected readonly Set<string> reparseReferencesSensitiveProperties = new Set<string>();
		
		/// <summary>
		/// A list of project properties that cause reparsing of code when they are changed.
		/// </summary>
		protected readonly Set<string> reparseCodeSensitiveProperties = new Set<string>();
		
		protected CompilableProject(IMSBuildEngineProvider engineProvider)
			: base(engineProvider.BuildEngine)
		{
		}
		
		protected override void Create(ICSharpCode.SharpDevelop.Internal.Templates.ProjectCreateInformation information)
		{
			base.Create(information);
			
			this.OutputType = OutputType.Exe;
			this.RootNamespace = information.RootNamespace;
			this.AssemblyName = information.ProjectName;
			
			SetProperty("Debug", null, "OutputPath", @"bin\Debug\",
			            PropertyStorageLocations.ConfigurationSpecific, true);
			SetProperty("Release", null, "OutputPath", @"bin\Release\",
			            PropertyStorageLocations.ConfigurationSpecific, true);
			
			SetProperty("Debug", null, "DebugSymbols", "True",
			            PropertyStorageLocations.ConfigurationSpecific, true);
			SetProperty("Release", null, "DebugSymbols", "False",
			            PropertyStorageLocations.ConfigurationSpecific, true);
			
			SetProperty("Debug", null, "DebugType", "Full",
			            PropertyStorageLocations.ConfigurationSpecific, true);
			SetProperty("Release", null, "DebugType", "None",
			            PropertyStorageLocations.ConfigurationSpecific, true);
		}
		
		/// <summary>
		/// Gets the path where temporary files are written to during compilation.
		/// </summary>
		[Browsable(false)]
		public string IntermediateOutputFullPath {
			get {
				string outputPath = GetEvaluatedProperty("IntermediateOutputPath");
				if (string.IsNullOrEmpty(outputPath)) {
					outputPath = GetEvaluatedProperty("BaseIntermediateOutputPath");
					if (string.IsNullOrEmpty(outputPath)) {
						outputPath = "obj";
					}
					outputPath = Path.Combine(outputPath, this.ActiveConfiguration);
				}
				return Path.Combine(Directory, outputPath);
			}
		}
		
		/// <summary>
		/// Gets the full path to the xml documentation file generated by the project, or
		/// <c>null</c> if no xml documentation is being generated.
		/// </summary>
		[Browsable(false)]
		public string DocumentationFileFullPath {
			get {
				string file = GetEvaluatedProperty("DocumentationFile");
				if (string.IsNullOrEmpty(file))
					return null;
				return Path.Combine(Directory, file);
			}
		}
		
		// Make Language abstract again to ensure backend-binding implementers don't forget
		// to set it.
		public abstract override string Language {
			get;
		}
		
		public abstract override ICSharpCode.SharpDevelop.Dom.LanguageProperties LanguageProperties {
			get;
		}
		
		public override string AssemblyName {
			get { return GetEvaluatedProperty("AssemblyName") ?? Name; }
			set { SetProperty("AssemblyName", value); }
		}
		
		public override string RootNamespace {
			get { return GetEvaluatedProperty("RootNamespace") ?? ""; }
			set { SetProperty("RootNamespace", value); }
		}
		
		public override string OutputAssemblyFullPath {
			get {
				string outputPath = GetEvaluatedProperty("OutputPath") ?? "";
				return Path.Combine(Path.Combine(Directory, outputPath), AssemblyName + GetExtension(OutputType));
			}
		}
		
		[Browsable(false)]
		public OutputType OutputType {
			get {
				try {
					return (OutputType)Enum.Parse(typeof(OutputType), GetEvaluatedProperty("OutputType") ?? "Exe");
				} catch (ArgumentException) {
					return OutputType.Exe;
				}
			}
			set {
				SetProperty("OutputType", value.ToString());
			}
		}
		
		protected override ParseProjectContent CreateProjectContent()
		{
			return ParseProjectContent.CreateUninitalized(this);
		}
		
		#region Starting (debugging)
		public override bool IsStartable {
			get {
				switch (this.StartAction) {
					case StartAction.Project:
						return OutputType == OutputType.Exe || OutputType == OutputType.WinExe;
					case StartAction.Program:
						return this.StartProgram.Length > 0;
					case StartAction.StartURL:
						return this.StartUrl.Length > 0;
					default:
						return false;
				}
			}
		}
		
		protected void Start(string program, bool withDebugging)
		{
			ProcessStartInfo psi = new ProcessStartInfo();
			psi.FileName = Path.Combine(Directory, program);
			string workingDir = StringParser.Parse(this.StartWorkingDirectory);
			if (workingDir.Length == 0) {
				psi.WorkingDirectory = Path.GetDirectoryName(psi.FileName);
			} else {
				psi.WorkingDirectory = Path.Combine(Directory, workingDir);
			}
			psi.Arguments = StringParser.Parse(this.StartArguments);
			
			if (!File.Exists(psi.FileName)) {
				MessageService.ShowError(psi.FileName + " does not exist and cannot be started.");
				return;
			}
			if (!System.IO.Directory.Exists(psi.WorkingDirectory)) {
				MessageService.ShowError("Working directory " + psi.WorkingDirectory + " does not exist; the process cannot be started. You can specify the working directory in the project options.");
				return;
			}
			
			if (withDebugging) {
				DebuggerService.CurrentDebugger.Start(psi);
			} else {
				DebuggerService.CurrentDebugger.StartWithoutDebugging(psi);
			}
		}
		
		public override void Start(bool withDebugging)
		{
			switch (this.StartAction) {
				case StartAction.Project:
					Start(this.OutputAssemblyFullPath, withDebugging);
					break;
				case StartAction.Program:
					Start(this.StartProgram, withDebugging);
					break;
				case StartAction.StartURL:
					FileService.OpenFile("browser://" + this.StartUrl);
					break;
				default:
					throw new System.ComponentModel.InvalidEnumArgumentException("StartAction", (int)this.StartAction, typeof(StartAction));
			}
		}
		
		[Browsable(false)]
		public string StartProgram {
			get {
				return GetEvaluatedProperty("StartProgram") ?? "";
			}
			set {
				SetProperty("StartProgram", string.IsNullOrEmpty(value) ? null : value);
			}
		}
		
		[Browsable(false)]
		public string StartUrl {
			get {
				return GetEvaluatedProperty("StartURL") ?? "";
			}
			set {
				SetProperty("StartURL", string.IsNullOrEmpty(value) ? null : value);
			}
		}
		
		[Browsable(false)]
		public StartAction StartAction {
			get {
				try {
					return (StartAction)Enum.Parse(typeof(StartAction), GetEvaluatedProperty("StartAction") ?? "Project");
				} catch (ArgumentException) {
					return StartAction.Project;
				}
			}
			set {
				SetProperty("StartAction", value.ToString());
			}
		}
		
		[Browsable(false)]
		public string StartArguments {
			get {
				return GetEvaluatedProperty("StartArguments") ?? "";
			}
			set {
				SetProperty("StartArguments", string.IsNullOrEmpty(value) ? null : value);
			}
		}
		
		[Browsable(false)]
		public string StartWorkingDirectory {
			get {
				return GetEvaluatedProperty("StartWorkingDirectory") ?? "";
			}
			set {
				SetProperty("StartWorkingDirectory", string.IsNullOrEmpty(value) ? null : value);
			}
		}
		#endregion
		
		protected override void OnActiveConfigurationChanged(EventArgs e)
		{
			base.OnActiveConfigurationChanged(e);
			if (!isLoading) {
				ParserService.Reparse(this, true, true);
			}
		}
		
		protected override void OnActivePlatformChanged(EventArgs e)
		{
			base.OnActivePlatformChanged(e);
			if (!isLoading) {
				ParserService.Reparse(this, true, true);
			}
		}
		
		protected override void OnPropertyChanged(ProjectPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (reparseReferencesSensitiveProperties.Contains(e.PropertyName)) {
				ParserService.Reparse(this, true, false);
			}
			if (reparseCodeSensitiveProperties.Contains(e.PropertyName)) {
				ParserService.Reparse(this, false, true);
			}
		}
		
		[Browsable(false)]
		public override string TypeGuid {
			get {
				return LanguageBindingService.GetCodonPerLanguageName(Language).Guid;
			}
			set {
				throw new NotSupportedException();
			}
		}
		
		public override ItemType GetDefaultItemType(string fileName)
		{
			string extension = Path.GetExtension(fileName);
			if (".resx".Equals(extension, StringComparison.OrdinalIgnoreCase)
			    || ".resources".Equals(extension, StringComparison.OrdinalIgnoreCase))
			{
				return ItemType.EmbeddedResource;
			} else if (".xaml".Equals(extension, StringComparison.OrdinalIgnoreCase)) {
				return ItemType.Page;
			} else {
				return base.GetDefaultItemType(fileName);
			}
		}
	}
}

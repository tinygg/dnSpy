﻿/*
    Copyright (C) 2014-2017 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Breakpoints.Code.TextEditor;
using dnSpy.Contracts.Debugger.DotNet.CorDebug.Code;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.CorDebug.Code;
using dnSpy.Debugger.CorDebug.Properties;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Debugger.CorDebug.Breakpoints.TextEditor {
	[ExportBreakpointGlyphFormatter]
	sealed class BreakpointGlyphFormatterImpl : BreakpointGlyphFormatter {
		readonly IDecompilerService decompilerService;

		[ImportingConstructor]
		BreakpointGlyphFormatterImpl(IDecompilerService decompilerService) => this.decompilerService = decompilerService;

		public override bool WriteLocation(ITextColorWriter output, DbgCodeBreakpoint breakpoint, ITextView textView, SnapshotSpan span) {
			if (breakpoint.Location is DbgDotNetNativeCodeLocationImpl location)
				return WriteLocation(output, textView, span, location);

			return false;
		}

		bool WriteLocation(ITextColorWriter output, ITextView textView, SnapshotSpan span, DbgDotNetNativeCodeLocationImpl location) {
			var line = span.Start.GetContainingLine();
			output.Write(BoxedTextColor.Text, string.Format(dnSpy_Debugger_CorDebug_Resources.GlyphToolTip_line_0_character_1,
				(line.LineNumber + 1).ToString(CultureInfo.CurrentUICulture),
				(span.Start - line.Start + 1).ToString(CultureInfo.CurrentUICulture)));
			output.WriteSpace();
			switch (location.ILOffsetMapping) {
			case DbgILOffsetMapping.Exact:
			case DbgILOffsetMapping.Approximate:
				var prefix = location.ILOffsetMapping == DbgILOffsetMapping.Approximate ? "~0x" : "0x";
				output.Write(BoxedTextColor.Text, string.Format(dnSpy_Debugger_CorDebug_Resources.GlyphToolTip_IL_offset_0, prefix + location.ILOffset.ToString("X4")));
				break;

			case DbgILOffsetMapping.Prolog:
				output.Write(BoxedTextColor.Text, string.Format(dnSpy_Debugger_CorDebug_Resources.GlyphToolTip_IL_offset_0, "(prolog)"));
				break;

			case DbgILOffsetMapping.Epilog:
				output.Write(BoxedTextColor.Text, string.Format(dnSpy_Debugger_CorDebug_Resources.GlyphToolTip_IL_offset_0, "(epilog)"));
				break;

			case DbgILOffsetMapping.Unknown:
			case DbgILOffsetMapping.NoInfo:
			case DbgILOffsetMapping.UnmappedAddress:
				output.Write(BoxedTextColor.Text, string.Format(dnSpy_Debugger_CorDebug_Resources.GlyphToolTip_IL_offset_0, "(???)"));
				break;

			default: throw new InvalidOperationException();
			}
			output.WriteSpace();
			var addr = location.NativeMethodAddress + location.NativeMethodOffset;
			output.Write(BoxedTextColor.Text, string.Format(dnSpy_Debugger_CorDebug_Resources.GlyphToolTip_NativeAddress, "0x" + addr.ToString("X8")));

			var documentViewer = textView.TextBuffer.TryGetDocumentViewer();
			Debug.Assert(documentViewer != null);
			var statement = documentViewer?.GetMethodDebugService().FindByCodeOffset(new ModuleTokenId(location.Module, location.Token), location.ILOffset);
			Debug.Assert((documentViewer != null) == (statement != null));
			if (statement != null) {
				output.Write(BoxedTextColor.Text, " ('");
				var decompiler = (documentViewer?.DocumentTab.Content as IDecompilerTabContent)?.Decompiler ?? decompilerService.Decompiler;
				decompiler.Write(output, statement.Value.Method, SimplePrinterFlags.Default);
				output.Write(BoxedTextColor.Text, "')");
			}

			return true;
		}
	}
}

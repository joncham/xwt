// 
// TreeViewBackend.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc
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
using Xwt.Backends;

namespace Xwt.GtkBackend
{
	public class TreeViewBackend: TableViewBackend, ITreeViewBackend
	{
		Gtk.TreePath autoExpandPath;
		uint expandTimer;
		
		protected override void OnSetDragTarget (Gtk.TargetEntry[] table, Gdk.DragAction actions)
		{
			base.OnSetDragTarget (table, actions);
			Widget.EnableModelDragDest (table, actions);
		}
		
		protected override void OnSetDragSource (Gdk.ModifierType modifierType, Gtk.TargetEntry[] table, Gdk.DragAction actions)
		{
			base.OnSetDragSource (modifierType, table, actions);
			Widget.EnableModelDragSource (modifierType, table, actions);
		}
		
		protected override void OnSetDragStatus (Gdk.DragContext context, int x, int y, uint time, Gdk.DragAction action)
		{
			base.OnSetDragStatus (context, x, y, time, action);
			
			// We are overriding the TreeView methods for handling drag & drop, so we need
			// to manually highlight the selected row
			
			Gtk.TreeViewDropPosition tpos;
			Gtk.TreePath path;
			if (!Widget.GetDestRowAtPos (x, y, out path, out tpos))
				path = null;
			
			if (expandTimer == 0 || autoExpandPath != path) {
				if (expandTimer != 0)
					GLib.Source.Remove (expandTimer);
				if (path != null) {
					expandTimer = GLib.Timeout.Add (600, delegate {
						Widget.ExpandRow (path, false);
						return false;
					});
				}
				autoExpandPath = path;
			}
			
			if (path != null && action != 0)
				Widget.SetDragDestRow (path, tpos);
			else
				Widget.SetDragDestRow (null, 0);
		}
		
		public override void Dispose (bool disposing)
		{
			if (expandTimer != 0)
				GLib.Source.Remove (expandTimer);
			base.Dispose (disposing);
		}
		
		public void SetSource (ITreeDataSource source, IBackend sourceBackend)
		{
			TreeStoreBackend b = sourceBackend as TreeStoreBackend;
			if (b == null) {
				CustomTreeModel model = new CustomTreeModel (source);
				Widget.Model = model.Store;
			} else
				Widget.Model = b.Store;
		}

		public TreePosition[] SelectedRows {
			get {
				var rows = Widget.Selection.GetSelectedRows ();
				IterPos[] sel = new IterPos [rows.Length];
				for (int i = 0; i < rows.Length; i++) {
					Gtk.TreeIter it;
					Widget.Model.GetIter (out it, rows[i]);
					sel[i] = new IterPos (-1, it);
				}
				return sel;
			}
		}
		
		public void SelectRow (TreePosition pos)
		{
			Widget.Selection.SelectIter (((IterPos)pos).Iter);
		}
		
		public void UnselectRow (TreePosition pos)
		{
			Widget.Selection.UnselectIter (((IterPos)pos).Iter);
		}
		
		public bool IsRowSelected (TreePosition pos)
		{
			return Widget.Selection.IterIsSelected (((IterPos)pos).Iter);
		}
		
		public bool IsRowExpanded (TreePosition pos)
		{
			return Widget.GetRowExpanded (Widget.Model.GetPath (((IterPos)pos).Iter));
		}
		
		public void ExpandRow (TreePosition pos, bool expandedChildren)
		{
			Widget.ExpandRow (Widget.Model.GetPath (((IterPos)pos).Iter), expandedChildren);
		}
		
		public void CollapseRow (TreePosition pos)
		{
			Widget.CollapseRow (Widget.Model.GetPath (((IterPos)pos).Iter));
		}
		
		public void ScrollToRow (TreePosition pos)
		{
			Widget.ScrollToCell (Widget.Model.GetPath (((IterPos)pos).Iter), Widget.Columns[0], false, 0, 0);
		}
		
		public void ExpandToRow (TreePosition pos)
		{
			Widget.ExpandToPath (Widget.Model.GetPath (((IterPos)pos).Iter));
		}
		
		public bool HeadersVisible {
			get {
				return Widget.HeadersVisible;
			}
			set {
				Widget.HeadersVisible = value;
			}
		}
		
		public bool GetDropTargetRow (double x, double y, out RowDropPosition pos, out TreePosition nodePosition)
		{
			Gtk.TreeViewDropPosition tpos;
			Gtk.TreePath path;
			if (!Widget.GetDestRowAtPos ((int)x, (int)y, out path, out tpos)) {
				pos = RowDropPosition.Into;
				nodePosition = null;
				return false;
			}
			
			Gtk.TreeIter it;
			Widget.Model.GetIter (out it, path);
			nodePosition = new IterPos (-1, it);
			switch (tpos) {
			case Gtk.TreeViewDropPosition.After: pos = RowDropPosition.After; break;
			case Gtk.TreeViewDropPosition.Before: pos = RowDropPosition.Before; break;
			default: pos = RowDropPosition.Into; break;
			}
			return true;
		}
	}
}


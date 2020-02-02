using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

namespace EditorGUITable
{

	public class TableAttributeParsingException : System.SystemException
	{
		public TableAttributeParsingException (string message) : base (message) { }
	}

	/// <summary>
	/// Drawer for the Table Attribute.
	/// See the TableAttribute class documentation for the limitations of this attribute.
	/// </summary>
	[CustomPropertyDrawer(typeof(TableAttribute))]
	public class TableDrawer : PropertyDrawer
	{

		protected GUITableState tableState;

		Rect lastRect;

		GUITableOption[] _tableOptions;
		GUITableOption[] tableOptions
		{
			get
			{
				if (_tableOptions == null)
				{
					TableAttribute tableAttribute = (TableAttribute)attribute;
					_tableOptions = ParsingHelpers.ParseTableOptions(tableAttribute.tableOptions);
					_tableOptions = ForceTableOptions(_tableOptions);
				}
				return _tableOptions;
			}
		}

		public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
		{

			if (!IsFirstElementQuickCheck(property.propertyPath))
			{
				if (!IsCollectionQuickCheck(property.propertyPath))
					return EditorGUIUtility.singleLineHeight;
				return 0;
			}

			string collectionPath, index;

			if (!IsCollectionFullCheck(property.propertyPath, out index, out collectionPath))
				return EditorGUIUtility.singleLineHeight;

			if (index != "0")
				return 0;

			GUITableEntry tableEntry = new GUITableEntry(tableOptions);
			SerializedProperty collectionProperty = property.serializedObject.FindProperty(collectionPath);
			return collectionProperty.arraySize * (GUITable.RowHeight(tableEntry) - 2) + EditorGUIUtility.singleLineHeight + GUITable.HeadersHeight(tableEntry) + GUITable.ExtraHeight(tableEntry);

		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{

			if (!IsFirstElementQuickCheck(property.propertyPath))
			{
				if (!IsCollectionQuickCheck(property.propertyPath))
				{
					DrawNotACollectionMessage(position, property, label);
					return;
				}
				return;
			}

			string collectionPath, index;

			if (!IsCollectionFullCheck(property.propertyPath, out index, out collectionPath))
			{
				DrawNotACollectionMessage(position, property, label);
				return;
			}

			if (index != "0")
				return;

			// Sometimes GetLastRect returns 0, so we keep the last relevant value
			if (GUILayoutUtility.GetLastRect().width > 1f)
				lastRect = GUILayoutUtility.GetLastRect();

			SerializedProperty collectionProperty = property.serializedObject.FindProperty(collectionPath);

			EditorGUI.indentLevel = 0;

			Rect r = new Rect(position.x + 15f, position.y, position.width - 15f, lastRect.height);

			TableAttribute tableAttribute = (TableAttribute)attribute;

			tableState = DrawTable (r, collectionProperty, label, tableAttribute);
		}

		void DrawNotACollectionMessage(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.LabelField(position, label.text, "Use the Table attribute with a collection.");
		}

		bool IsCollectionQuickCheck(string properyPath)
		{
			return properyPath.Contains("Array.data[");
		}

		bool IsFirstElementQuickCheck(string properyPath)
		{
			return properyPath.EndsWith("[0]");
		}

		Match CollectionPropertyPathRegex(string propertyPath)
		{
			return Regex.Match(propertyPath, "^([a-zA-Z0-9_]*).Array.data\\[([0-9]*)\\]$");
		}

		bool IsCollectionFullCheck(string propertyPath, out string indexString, out string collectionPath)
		{
			indexString = collectionPath = string.Empty;

			Match match = Regex.Match(propertyPath, "^([a-zA-Z0-9_]*).Array.data\\[([0-9]*)\\]$");

			if (!match.Success)
				return false;

			collectionPath = match.Groups[1].Value;
			indexString = match.Groups[2].Value;
			return true;

		}

		protected virtual GUITableState DrawTable (Rect rect, SerializedProperty collectionProperty, GUIContent label, TableAttribute tableAttribute)
		{
			try
			{
				if (tableAttribute.properties == null)
					return GUITable.DrawTable(rect, tableState, collectionProperty, tableOptions);
				else
					return GUITable.DrawTable(rect, tableState, collectionProperty, tableAttribute.properties.ToList(), tableOptions);
			}
			catch (TableAttributeParsingException e)
			{
				Debug.LogError (e.Message + "\nGiving up drawing table for " + collectionProperty.name );
				return tableState;
			}
		}

		GUITableOption[] ForceTableOptions (GUITableOption[] options)
		{
			return options
				.Concat (forcedTableOptions)
				.ToArray ();
		}

		protected virtual GUITableOption[] forcedTableOptions
		{
			get 
			{
				return new GUITableOption[] {GUITableOption.AllowScrollView(false)};
			}
		}

	}

}
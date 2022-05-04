//
// CommandLineParser.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Hyena.CommandLine
{
	public class CommandLineParser
	{
		struct Argument
		{
			public int Order;
			public string Value;

			public Argument (int order, string value)
			{
				Order = order;
				Value = value;
			}
		}

		int generation;
		int sorted_args_generation;
		int offset;
		string[] arguments;
		public string[] AllArguments { get => arguments; }
		KeyValuePair<string, Argument>[] sorted_args;
		Dictionary<string, Argument> parsed_arguments = new Dictionary<string, Argument> ();
		List<string> file_list = new List<string> ();

		public CommandLineParser () : this (Environment.GetCommandLineArgs (), 1)
		{
		}

		public CommandLineParser (string[] arguments, int offset)
		{
			this.arguments = arguments;
			this.offset = offset;

			Parse ();
		}

		void Parse ()
		{
			for (int i = offset; i < arguments.Length; i++) {
				if (!IsOption (arguments[i])) {
					file_list.Add (arguments[i]);
					continue;
				}

				string name = OptionName (arguments[i]);
				string value = string.Empty;

				int eq_offset = name.IndexOf ('=');
				if (eq_offset > 1) {
					value = name.Substring (eq_offset + 1);
					name = name.Substring (0, eq_offset);
				}

				if (parsed_arguments.ContainsKey (name)) {
					parsed_arguments[name] = new Argument (i, value);
				} else {
					parsed_arguments.Add (name, new Argument (i, value));
				}
			}
		}

		bool IsOption (string argument)
		{
			return argument.Length > 2 && argument.Substring (0, 2) == "--";
		}

		string OptionName (string argument)
		{
			return argument.Substring (2);
		}

		public bool Contains (string name)
		{
			return parsed_arguments.ContainsKey (name);
		}

		public bool ContainsStart (string start)
		{
			foreach (string argument in parsed_arguments.Keys) {
				if (argument.StartsWith (start)) {
					return true;
				}
			}
			return false;
		}

		public string this[string name] {
			get { return Contains (name) ? parsed_arguments[name].Value : string.Empty; }
			set {
				Argument arg = parsed_arguments[name];
				arg.Value = value;
				parsed_arguments[name] = arg;
				generation++;
			}
		}

		public IEnumerable<KeyValuePair<string, string>> Arguments {
			get {
				if (sorted_args == null || sorted_args_generation != generation) {
					sorted_args = new KeyValuePair<string, Argument>[parsed_arguments.Count];
					int i = 0;

					foreach (KeyValuePair<string, Argument> arg in parsed_arguments) {
						sorted_args[i++] = arg;
					}

					Array.Sort (sorted_args, delegate (KeyValuePair<string, Argument> a, KeyValuePair<string, Argument> b) {
						return a.Value.Order.CompareTo (b.Value.Order);
					});

					sorted_args_generation = generation;
				}

				foreach (KeyValuePair<string, Argument> arg in sorted_args) {
					yield return new KeyValuePair<string, string> (arg.Key, arg.Value.Value);
				}
			}
		}

		public ReadOnlyCollection<string> Files {
			get { return new ReadOnlyCollection<string> (file_list); }
		}

		public override string ToString ()
		{
			var builder = new System.Text.StringBuilder ();

			builder.Append ("Parsed Arguments\n");
			foreach (KeyValuePair<string, Argument> argument in parsed_arguments) {
				builder.AppendFormat ("  {0} = [{1}]\n", argument.Key, argument.Value.Value);
			}

			builder.Append ("\nFile List\n");
			foreach (string file in file_list) {
				builder.AppendFormat ("{0}\n", file);
			}

			return builder.ToString ();
		}
	}
}

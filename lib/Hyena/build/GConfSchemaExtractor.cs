using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;

public class GConfSchemaExtractor
{
	private static Dictionary<string, StringBuilder> entries = 
		new Dictionary<string, StringBuilder>();
    private static int schema_count = 0;

    public static void Main(string [] args)
    {
        Assembly asm = Assembly.LoadFrom(args[0]);
        foreach(Type type in asm.GetTypes()) {
            foreach(FieldInfo field in type.GetFields()) {
                if(field.FieldType.IsGenericType && 
                    field.FieldType.GetGenericTypeDefinition().Name.StartsWith("SchemaEntry")) {
                    
                    if(field.Name == "Zero") {
                        continue;
                    }

                    object schema = field.GetValue(null);
                
                    AddSchemaEntry(schema.GetType().GetField("DefaultValue").GetValue(schema),
                        GetString(schema, "Namespace"),
                        GetString(schema, "Key"),
                        GetString(schema, "ShortDescription"),
                        GetString(schema, "LongDescription")
                    );
                }
            }
        }

        if(schema_count > 0) {
            StringBuilder final = new StringBuilder();
            final.Append("<?xml version=\"1.0\"?>\n");
            final.Append("<gconfschemafile>\n");
            final.Append("  <schemalist>\n");

			List<string> keys = new List<string>(entries.Keys);
			keys.Sort();

			foreach(string key in keys) {
				final.Append(entries[key]);
			}
			
            final.Append("  </schemalist>\n");
            final.Append("</gconfschemafile>\n");

            using(StreamWriter writer = new StreamWriter(args[1])) {
                writer.Write(final.ToString());
            }
        }
    }

    private static string GetString(object o, string name)
    {
        FieldInfo field = o.GetType().GetField(name);
        return (string)field.GetValue(o);
    }

    private static string GetValueString(Type type, object o, out string gctype)
    {
        if(type == typeof(bool)) {
            gctype = "bool";
            return o == null ? null : o.ToString().ToLower();
        } else if(type == typeof(int)) {
            gctype = "int";
        } else if(type == typeof(float) || type == typeof(double)) {
            gctype = "float";
        } else if(type == typeof(string)) {
            gctype = "string";
        } else {
            throw new Exception("Unsupported type '" + type + "'");
        }
        
        return o == null ? null : o.ToString();
    }

    private static void AddSchemaEntry(object value, string namespce, string key, 
        string short_desc, string long_desc)
    {
        schema_count++;
        
        string full_key = CreateKey(namespce, key);
        
        bool list = value.GetType().IsArray;
        Type type = list ? Type.GetTypeArray((object [])value)[0] : value.GetType();
        string str_val = null;
        string str_type = null;
        
        if(list) {
            if(value == null || ((object [])value).Length == 0) {
                GetValueString(type, null, out str_type);
                str_val = "[]";
            } else {
                str_val = "[";
                object [] arr = (object [])value;
                for(int i = 0; i < arr.Length; i++) {
                    str_val += GetValueString(type, arr[i], out str_type).Replace(",", "\\,");
                    if(i < arr.Length - 1) {
                        str_val += ",";
                    }
                }
                str_val += "]";
            }
        } else {
            str_val = GetValueString(type, value, out str_type);
        }
 
 		StringBuilder builder = new StringBuilder();
        builder.AppendFormat("    <schema>\n");
        builder.AppendFormat("      <key>/schemas{0}</key>\n", full_key);
        builder.AppendFormat("      <applyto>{0}</applyto>\n", full_key);
        builder.AppendFormat("      <owner>banshee</owner>\n");
        if(!list) {
            builder.AppendFormat("      <type>{0}</type>\n", str_type);
        } else {
            builder.AppendFormat("      <type>list</type>\n");
            builder.AppendFormat("      <list_type>{0}</list_type>\n", str_type);
        }
        builder.AppendFormat("      <default>{0}</default>\n", str_val);
        builder.AppendFormat("      <locale name=\"C\">\n");
        builder.AppendFormat("        <short>{0}</short>\n", short_desc);
        builder.AppendFormat("        <long>{0}</long>\n", long_desc);
        builder.AppendFormat("      </locale>\n");
        builder.AppendFormat("    </schema>\n");
		entries.Add(full_key, builder);
    }
        
    private static string CamelCaseToUnderCase(string s)
    {
        string undercase = String.Empty;
        string [] tokens = Regex.Split(s, "([A-Z]{1}[a-z]+)");
        
        for(int i = 0; i < tokens.Length; i++) {
            if(tokens[i] == String.Empty) {
                continue;
            }

            undercase += tokens[i].ToLower();
            if(i < tokens.Length - 2) {
                undercase += "_";
            }
        }
        
        return undercase;
    }

    private static string CreateKey(string namespce, string key)
    {
        return namespce == null 
            ? "/apps/banshee/" + CamelCaseToUnderCase(key)
            : "/apps/banshee/" + CamelCaseToUnderCase(namespce.Replace(".", "/")) 
                + "/" + CamelCaseToUnderCase(key);
    } 
}


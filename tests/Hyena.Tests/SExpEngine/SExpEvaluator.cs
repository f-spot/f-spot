using System;
using System.IO;
using Hyena.SExpEngine;

public class SExpEvaluator
{
    public static void Main(string [] args)
    {
        using(FileStream stream = new FileStream(args[0], FileMode.Open)) {
            using(StreamReader reader = new StreamReader(stream)) {
                Evaluator evaluator = new Evaluator();
                TreeNode result = evaluator.EvaluateString(reader.ReadToEnd()).Flatten();

                if(evaluator.Success) {
                    Console.WriteLine("[[=== Result Tree ===]]");
                    if(!result.Empty) {
                        result.Dump();
                    } else {
                        Console.WriteLine("No result");
                    }
                } else {
                    Console.WriteLine(evaluator.ErrorMessage);
                    foreach(Exception exception in evaluator.Exceptions) {
                        Console.WriteLine(exception.Message);
                    }
                }
	       }
        }
    }
}

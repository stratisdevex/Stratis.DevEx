using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stratis.VS.StratisEVM
{

    public class SlitherAnalysis
    {
        public bool success { get; set; }
        public object error { get; set; }
        public Results results { get; set; }
    }

    public class Results
    {
        public Detector[] detectors { get; set; }
    }

    public class Detector
    {
        public Element[] elements { get; set; }
        public string description { get; set; }
        public string markdown { get; set; }
        public string first_markdown_element { get; set; }
        public string id { get; set; }
        public string check { get; set; }
        public string impact { get; set; }
        public string confidence { get; set; }
    }

    public class Element
    {
        public string type { get; set; }
        public string name { get; set; }
        public Source_Mapping source_mapping { get; set; }
        public Type_Specific_Fields type_specific_fields { get; set; }
    }

    public class Source_Mapping
    {
        public int start { get; set; }
        public int length { get; set; }
        public string filename_relative { get; set; }
        public string filename_absolute { get; set; }
        public string filename_short { get; set; }
        public bool is_dependency { get; set; }
        public int[] lines { get; set; }
        public int starting_column { get; set; }
        public int ending_column { get; set; }
    }

    public class Type_Specific_Fields
    {
        public Parent parent { get; set; }
        public string signature { get; set; }
    }

    public class Parent
    {
        public string type { get; set; }
        public string name { get; set; }
        public Source_Mapping1 source_mapping { get; set; }
    }

    public class Source_Mapping1
    {
        public int start { get; set; }
        public int length { get; set; }
        public string filename_relative { get; set; }
        public string filename_absolute { get; set; }
        public string filename_short { get; set; }
        public bool is_dependency { get; set; }
        public int[] lines { get; set; }
        public int starting_column { get; set; }
        public int ending_column { get; set; }
    }

}

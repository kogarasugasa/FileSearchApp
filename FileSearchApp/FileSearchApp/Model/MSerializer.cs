using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
using System.Linq.Expressions;
using System.Windows.Forms.VisualStyles;

namespace FileSearchApp
{
    public static class nameof
    {
        public static string Get<MemberType>(Expression<Func<MemberType>> expression)
        {
            if (expression.Body.GetType() == typeof(MemberExpression)){
                var ex = (MemberExpression)expression.Body;
                return ex.Member.Name;
            }
            return "";
        }
    }
    public abstract class Serializer
    {
        protected enum Fields { ClassName, Property, Value }
        protected object _vm;
        public Serializer(object vm)
        {
            _vm = vm;
        }
        protected abstract string Serialize<T>(T pVm);
        public string Serialize()
        {
            return this.Serialize(_vm);
        }
    }
    public class MFileSearchSerializer : Serializer
    {
        public MFileSearchSerializer(VMFileSearchApp vm) : base(vm) {}
        protected override string Serialize<T>(T pVm)
        {
            var vm = (VMFileSearchApp)_vm;
            var list = new List<string>(){
                Fields.ClassName.ToString() + ":" + typeof(T).Name,
                Fields.Property.ToString() + ":" + nameof.Get(() => vm.SearchWords),
                Fields.Value.ToString() + ":" + vm.SearchWords,
            };
            return string.Join(Environment.NewLine, list);
        }
        public bool Deserialize(ref VMFileSearchApp pVm, IEnumerable<string> pList)
        {
            var dic = this.Deserialize(pList);
            if (dic.Count == 0){
                return false;
            }
            foreach (var item in dic)
            {
                var type = typeof(VMFileSearchApp);
                var props = type.GetProperties().Select(val => val.Name);
                if (props.Contains(item.Key)){
                    type.GetProperty(item.Key).SetValue(pVm, item.Value);
                }
            }
            return true;
        }
        Dictionary<string, string> Deserialize(IEnumerable<string> pList)
        {
            var list = pList.ToList();
            if (list[0].StartsWith(Fields.ClassName.ToString() + ":")
            && list[1].StartsWith(Fields.Property.ToString() + ":")
            && list[2].StartsWith(Fields.Value.ToString() + ":")){
                var dic = new Dictionary<string, string>();
                dic.Add(
                    Fields.ClassName.ToString(),
                    list[0].Substring(Fields.ClassName.ToString().Length)
                );
                dic.Add(
                    Fields.Property.ToString(),
                    list[1].Substring(Fields.Property.ToString().Length)
                );
                dic.Add(
                    Fields.Value.ToString(),
                    list[2].Substring(Fields.Value.ToString().Length)
                );
                return dic;
            }
            return new Dictionary<string, string>();
        }
    }
    public class MListViewSerializer
    {
        VMListView _vm;
        public MListViewSerializer(VMListView pVm)
        {
            _vm = pVm;
        }
    }
    public class MTabSerializer
    {

    }
}
using AddyScript.Runtime.DataItems;


namespace AddyScript.Translators.Utility;


public interface IIntrospectionHelper
{
    DataItem IsSubclassOf(DataItem sourceTypeInfo, DataItem targetTypeInfo);

    DataItem IsAssignableTo(DataItem sourceTypeInfo, DataItem targetTypeInfo);

    DataItem NewInstance(DataItem typeInfo, DataItem arguments);

    DataItem GetValue(DataItem memberInfo, DataItem target);

    DataItem SetValue(DataItem memberInfo, DataItem target, DataItem value);

    DataItem GetItem(DataItem propertyInfo, DataItem target, DataItem index);

    DataItem SetItem(DataItem propertyInfo, DataItem target, DataItem index, DataItem value);

    DataItem Invoke(DataItem methodInfo, DataItem target, DataItem arguments);
}

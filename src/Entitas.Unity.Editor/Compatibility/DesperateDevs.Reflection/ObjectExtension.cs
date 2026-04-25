namespace Entitas.Unity.Editor.Compatibility.Reflection
{
    public static class ObjectExtension
    {
        public static void CopyPublicMemberValues(this object source, object target)
        {
            var memberInfos = source.GetType().GetPublicMemberInfos();
            for (var i = 0; i < memberInfos.Length; i++)
            {
                var memberInfo = memberInfos[i];
                memberInfo.SetValue(target, memberInfo.GetValue(source));
            }
        }
    }
}

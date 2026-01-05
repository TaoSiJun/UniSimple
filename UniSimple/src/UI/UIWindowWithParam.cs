namespace UniSimple.UI
{
    /// <summary>
    /// 带参数
    /// </summary>
    public abstract class UIWindowWithParam<TParam> : UIWindow where TParam : UIParam
    {
        protected abstract void OnParam(TParam param);

        public override void OnOpen(UIParam param)
        {
            OnParam(param as TParam);
        }
    }
}
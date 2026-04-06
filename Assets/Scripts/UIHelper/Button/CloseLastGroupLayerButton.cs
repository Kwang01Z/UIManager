public class CloseLastGroupLayerButton : ButtonBase
{
    protected override void OnButtonClicked()
    {
        LayerManager.Instance.CloseLastLayerGroup();
    }
}

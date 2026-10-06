public class TakeOffAction : AnimationAction
{
    // 비용 1: 이륙 + 공중 브레스(2)가 지상 브레스(2)와 같아서, 2페이즈에선 둘 중 하나가 랜덤으로 선택된다
    public TakeOffAction() : base("TakeOff", 1, "TakeOff")
    {
        AddPrecondition(DragonKeys.IsFlying, false);
        AddPrecondition(DragonKeys.IsEnraged, true);

        AddEffect(DragonKeys.IsFlying, true);
    }

    protected override void Enter(DragonAgent dragon)
    {
        base.Enter(dragon);
        dragon.SetFlying(true);
    }
}

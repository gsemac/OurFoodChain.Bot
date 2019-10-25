function OnRegister(status)

	status.SetName("rooted")

	status.SetDuration(1)

end

function OnTurnEnd(args)

	args.Target.Stats.Hp = args.Target.Stats.Hp + (args.Target.Stats.MaxHp / 10)
	
	args.SetText("absorbed nutrients from its roots")

end
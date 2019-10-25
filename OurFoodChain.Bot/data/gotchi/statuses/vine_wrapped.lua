function OnRegister(status)

	status.SetName("vine-wrapped")

	status.SetSlipDamagePercent(0.16)

end

function OnTurnEnd(args)

	args.SetText("is damaged by vines")

end
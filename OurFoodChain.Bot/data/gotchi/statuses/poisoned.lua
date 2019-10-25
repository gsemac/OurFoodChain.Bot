function OnRegister(status)

	status.SetName("poisoned")

	status.SetSlipDamagePercent(0.16)

end

function OnTurnEnd(args)

	args.SetText("is damaged by poison")

end
-- #todo Damage should only be incurred when the user uses a damaging move.

function OnRegister(status)

	status.SetName("thorn-surrounded")

	status.SetSlipDamagePercent(0.10)

end

function OnTurnEnd(args)

	args.SetText("is damaged by thorns")

end
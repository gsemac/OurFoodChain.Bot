function onRegister(status)

	status.SetName("blinding")

	status.SetDuration(1)

end

function onTurnBegin(args)
	
	args.Opponent.Stats.Acc = 0.0

end

function onTurnEnd(args)

	args.Opponent.Stats.Acc = 1.0

end
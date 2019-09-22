function OnRegister(status)

	status.SetName("blinded")

	status.SetDuration(1)

end

function OnAcquire(args)

	args.Target.Stats.Acc = 0.0

end

function OnClear(args) 

	args.Target.Stats.Acc = 1.0

end
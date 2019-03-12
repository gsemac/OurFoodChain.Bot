function register(move)

	move.name = "zap";
	move.description = "Shocks the opponent with electricity. Highly effective against aquatic organisms.";

	move.pp = 20;
	move.type = type.Offensive;

end;

function callback(args) 

	sql = "SELECT COUNT(*) FROM Zones WHERE type = \"aquatic\" AND id IN (SELECT zone_id FROM SpeciesZones WHERE species_id = $id);";

	args.ifTargetMatchesSqlThen(sql, function(result)
		
		if(result) then
			args.applyDamage(1.2);
		else
			args.applyDamage();
		end;

	end);

end;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Models.Game
{
    public class GPcAbility
    {
		public short DDv { get; set; }
		public short MDv { get; set; }
		public short RDv { get; set; }
		public short DPv { get; set; }
		public short MPv { get; set; }
		public short RPv { get; set; }
		public short HidDDv { get; set; }
		public short HidMDv { get; set; }
		public short HidRDv { get; set; }
		public short HidDPv { get; set; }
		public short HidMPv { get; set; }
		public short HidRPv { get; set; }
		public short DDD { get; set; }
		public short DHit { get; set; }
		public short RDD { get; set; }
		public short RHit { get; set; }
		public short MDD { get; set; }
		public short MHit { get; set; }
		public short Str { get; set; }
		public short Dex { get; set; }
		public short Int { get; set; }
		public short AddDDWhenCritical { get; set; }
		public short SubDDWhenCritical { get; set; }
		public short CriticalHit { get; set; }
		public short EnemySubCriticalHit { get; set; }
		public short HpRegen { get; set; }
		public short MpRegen { get; set; }
		public short HwHpRegen { get; set; }
		public short HwMpRegen { get; set; }
		public short MaxHp { get; set; }
		public short MaxMp { get; set; }
		public short PvPDHIT { get; set; }
		public short PvPRHIT { get; set; }
		public short PvPMHIT { get; set; }
		// TODO: the ability does not yet keep the lists a worn set grants - bonuses against
		// a race, wards, elemental attack and resistance, inflicting and resisting abnormal
		// states; Reset below clears them once they exist


		public void Reset()
		{
			DDv = -1;
			MDv = -1;
			RDv = -1;
			DPv = 0;
			MPv = 0;
			MaxHp = 1;
			MaxMp = 1;
			RPv = 0;
			HidDDv = 0;
			HidMDv = 0;
			HidRDv = 0;
			HidDPv = 0;
			HidMPv = 0;
			HidRPv = 0;
			DDD = 0;
			DHit = 0;
			RDD = 0;
			RHit = 0;
			MDD = 0;
			MHit = 0;
			Str = 0;
			Dex = 0;
			Int = 0;
			CriticalHit = 0;
			EnemySubCriticalHit = 0;
			AddDDWhenCritical = 0;
			SubDDWhenCritical = 0;
			HpRegen = 0;
			MpRegen = 0;
			HwHpRegen = 0;
			HwMpRegen = 0;
			PvPDHIT = 0;
			PvPRHIT = 0;
			PvPMHIT = 0;
		}
	}
}

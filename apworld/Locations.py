from typing import Dict

location_table: Dict[str, int] = {
    # Score Milestones 
    **{f"Score Milestone {i}": 90000 + i for i in range(1, 29)},
    **{f"Manager Check {i}":    91000 + i for i in range(1, 51)},
    **{f"Biologist Check {i}":  92000 + i for i in range(1, 51)},
    **{f"Chemist Check {i}":    93000 + i for i in range(1, 51)},
    **{f"Trader Check {i}":     94000 + i for i in range(1, 51)},
    **{f"Astronomer Check {i}": 95000 + i for i in range(1, 51)},
    **{f"Big Eater Check {i}":  96000 + i for i in range(1, 51)},
}

location_name_to_id = location_table
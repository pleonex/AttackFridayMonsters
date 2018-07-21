#!/bin/python
#   Copyright 2017 Benito Palacios Sanchez (aka pleonex)
#
#   Licensed under the Apache License, Version 2.0 (the "License");
#   you may not use this file except in compliance with the License.
#   You may obtain a copy of the License at
#
#       http://www.apache.org/licenses/LICENSE-2.0
#
#   Unless required by applicable law or agreed to in writing, software
#   distributed under the License is distributed on an "AS IS" BASIS,
#   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
#   See the License for the specific language governing permissions and
#   limitations under the License.
"""Weblate checks for 'Attack of the Friday Monsters' game."""

from weblate.trans.checks.base import TargetCheckWithFlag
import re


class GeneralKeywordCheck(TargetCheckWithFlag):
    """General regex-keyword check and highlight."""

    check_id = 'keyword-regex'
    name = 'Text keywords'
    description = 'Keywords are missing or do not match'
    default_disabled = True
    severity = 'warning'
    compiled_regex = {}

    def get_regex(self, unit):
        """Get the regular expression for this unit."""
        # Get pair-value flags
        dict_flags = [f.split(':') for f in unit.all_flags if ':' in f]

        # Search our flag
        regex = None
        for flag in dict_flags:
            if flag[0] == self.check_id:
                regex = flag[1]
        if not regex:
            return None

        # Add the regex to the dict if it's not already
        if regex not in self.compiled_regex:
            self.compiled_regex[regex] = re.compile(regex)
        return self.compiled_regex[regex]

    def check_highlight(self, source, unit):
        """Highlight the keyword expressions from the flag."""
        regex = self.get_regex(unit)
        if not regex:
            return []

        # Return the matching if any
        return [(match.start(), match.end(), match.group())
                for match in regex.finditer(source)]

    def check_target_unit_with_flag(self, sources, targets, unit):
        """Check if the are the same number of tags."""
        regex = self.get_regex(unit)
        if not regex:
            return False

        return re.findall(regex, sources[0]) != re.findall(regex, targets[0])

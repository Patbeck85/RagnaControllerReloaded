#!/usr/bin/env python3
"""
RagnaController Changelog Generator
Generates changelog entries from git commits and release notes.
"""

import re
import sys
from pathlib import Path
from typing import List, Dict

def parse_git_log(git_output: str) -> List[Dict]:
    """Parse git log output and extract commit information."""
    commits = []
    
    # Pattern for git log format
    commit_pattern = r'^commit\s+([a-f0-9]+)\s*$'
    author_pattern = r'^Author:\s+(.+?)\s*<(.+?)>\s*$'
    date_pattern = r'^Date:\s+(.+?)\s*$'
    subject_pattern = r'^\s*(.+?)\s*$'
    
    current_commit = {}
    
    for line in git_output.split('\n'):
        match = re.match(commit_pattern, line)
        if match:
            # Save previous commit if exists
            if current_commit:
                commits.append(current_commit)
            
            current_commit = {
                'hash': match.group(1),
                'author': '',
                'date': '',
                'subject': '',
                'body': ''
            }
            continue
        
        match = re.match(author_pattern, line)
        if match and current_commit:
            current_commit['author'] = match.group(1)
            continue
        
        match = re.match(date_pattern, line)
        if match and current_commit:
            current_commit['date'] = match.group(1)
            continue
        
        # Check if line is empty (end of commit)
        if line.strip() == '' and current_commit:
            commits.append(current_commit)
            current_commit = {}
            continue
        
        # Subject line (first non-empty line after Date)
        if current_commit and line.strip() and not line.startswith(' '):
            current_commit['subject'] = line.strip()
            continue
        
        # Body lines (indented lines)
        if current_commit and line.startswith(' ') and line.strip():
            current_commit['body'] += line.strip() + '\n'
    
    # Don't forget the last commit
    if current_commit:
        commits.append(current_commit)
    
    return commits

def categorize_commits(commits: List[Dict]) -> Dict[str, List[Dict]]:
    """Categorize commits by type."""
    categories = {
        'features': [],
        'bugfixes': [],
        'performance': [],
        'ui_ux': [],
        'i18n': [],
        'documentation': [],
        'refactoring': [],
        'tests': [],
        'other': []
    }
    
    for commit in commits:
        subject = commit['subject'].lower()
        
        if any(x in subject for x in ['feat', 'feature', 'add', 'new']):
            categories['features'].append(commit)
        elif any(x in subject for x in ['fix', 'bug', 'resolve', 'repair']):
            categories['bugfixes'].append(commit)
        elif any(x in subject for x in ['perf', 'optimize', 'speed', 'fast']):
            categories['performance'].append(commit)
        elif any(x in subject for x in ['ui', 'ux', 'interface', 'design', 'theme']):
            categories['ui_ux'].append(commit)
        elif any(x in subject for x in ['i18n', 'localization', 'translate', 'locale', 'lang']):
            categories['i18n'].append(commit)
        elif any(x in subject for x in ['doc', 'readme', 'changelog', 'guide']):
            categories['documentation'].append(commit)
        elif any(x in subject for x in ['refactor', 'cleanup', 'restructure']):
            categories['refactoring'].append(commit)
        elif any(x in subject for x in ['test', 'coverage', 'stryker', 'xunit']):
            categories['tests'].append(commit)
        else:
            categories['other'].append(commit)
    
    return categories

def generate_changelog(commits: List[Dict], version: str = "1.0.0") -> str:
    """Generate a changelog from commits."""
    report = []
    report.append("=" * 70)
    report.append(f"RAGNACONTROLLER CHANGELOG - v{version}")
    report.append("=" * 70)
    report.append("")
    
    # Group by date
    from datetime import datetime
    dated_commits = {}
    for commit in commits:
        try:
            date_str = commit['date']
            # Parse date (format: "Mon Jan 15 12:00:00 2026 +0100")
            dt = datetime.strptime(date_str.split()[0], '%b %d %Y')
            date_key = dt.strftime('%Y-%m-%d')
            
            if date_key not in dated_commits:
                dated_commits[date_key] = []
            dated_commits[date_key].append(commit)
        except:
            # If date parsing fails, group by subject
            pass
    
    # Sort by date (newest first)
    sorted_dates = sorted(dated_commits.keys(), reverse=True)
    
    for date in sorted_dates:
        date_commits = dated_commits[date]
        report.append("-" * 70)
        report.append(f"{date}")
        report.append("-" * 70)
        
        # Sort commits within date by subject
        sorted_commits = sorted(date_commits, key=lambda x: x['subject'])
        
        for commit in sorted_commits:
            # Format subject
            subject = commit['subject']
            
            # Add emoji based on category
            if 'feat' in subject.lower():
                emoji = "✨"
            elif 'fix' in subject.lower():
                emoji = "🐛"
            elif 'perf' in subject.lower():
                emoji = "⚡"
            elif 'ui' in subject.lower() or 'ux' in subject.lower():
                emoji = "🎨"
            elif 'i18n' in subject.lower() or 'localization' in subject.lower():
                emoji = "🌐"
            elif 'doc' in subject.lower():
                emoji = "📚"
            elif 'refactor' in subject.lower():
                emoji = "🔧"
            elif 'test' in subject.lower():
                emoji = "✅"
            else:
                emoji = "📝"
            
            # Truncate long subjects
            if len(subject) > 80:
                subject = subject[:77] + "..."
            
            report.append(f"{emoji} {subject}")
        
        report.append("")
    
    report.append("=" * 70)
    report.append("END OF CHANGELOG")
    report.append("=" * 70)
    
    return '\n'.join(report)

def generate_release_notes(commits: List[Dict], version: str = "1.0.0") -> str:
    """Generate release notes from commits."""
    report = []
    report.append(f"# Release Notes - v{version}")
    report.append("")
    
    # Categorize commits
    categorized = categorize_commits(commits)
    
    if categorized['features']:
        report.append("## ✨ Features")
        for commit in categorized['features']:
            subject = commit['subject']
            if len(subject) > 80:
                subject = subject[:77] + "..."
            report.append(f"- {subject}")
        report.append("")
    
    if categorized['bugfixes']:
        report.append("## 🐛 Bug Fixes")
        for commit in categorized['bugfixes']:
            subject = commit['subject']
            if len(subject) > 80:
                subject = subject[:77] + "..."
            report.append(f"- {subject}")
        report.append("")
    
    if categorized['performance']:
        report.append("## ⚡ Performance")
        for commit in categorized['performance']:
            subject = commit['subject']
            if len(subject) > 80:
                subject = subject[:77] + "..."
            report.append(f"- {subject}")
        report.append("")
    
    if categorized['ui_ux']:
        report.append("## 🎨 UI/UX")
        for commit in categorized['ui_ux']:
            subject = commit['subject']
            if len(subject) > 80:
                subject = subject[:77] + "..."
            report.append(f"- {subject}")
        report.append("")
    
    if categorized['i18n']:
        report.append("## 🌐 Internationalization")
        for commit in categorized['i18n']:
            subject = commit['subject']
            if len(subject) > 80:
                subject = subject[:77] + "..."
            report.append(f"- {subject}")
        report.append("")
    
    if categorized['documentation']:
        report.append("## 📚 Documentation")
        for commit in categorized['documentation']:
            subject = commit['subject']
            if len(subject) > 80:
                subject = subject[:77] + "..."
            report.append(f"- {subject}")
        report.append("")
    
    if categorized['tests']:
        report.append("## ✅ Tests")
        for commit in categorized['tests']:
            subject = commit['subject']
            if len(subject) > 80:
                subject = subject[:77] + "..."
            report.append(f"- {subject}")
        report.append("")
    
    return '\n'.join(report)

def main():
    """Main entry point."""
    if len(sys.argv) < 2:
        print("Usage: generate_changelog.py <git_log_file> [version]")
        print("Example: generate_changelog.py git.log 1.0.0")
        sys.exit(1)
    
    git_log_file = Path(sys.argv[1])
    version = sys.argv[2] if len(sys.argv) > 2 else "1.0.0"
    
    if not git_log_file.exists():
        print(f"Error: File not found: {git_log_file}")
        sys.exit(1)
    
    with open(git_log_file, 'r', encoding='utf-8') as f:
        git_output = f.read()
    
    commits = parse_git_log(git_output)
    
    if not commits:
        print("No commits found in git log.")
        sys.exit(1)
    
    changelog = generate_changelog(commits, version)
    print(changelog)
    
    release_notes = generate_release_notes(commits, version)
    print("\n" + "="*70)
    print("RELEASE NOTES:")
    print("="*70)
    print(release_notes)
    
    # Save changelog to file
    changelog_file = git_log_file.parent / "CHANGELOG.md"
    with open(changelog_file, 'w', encoding='utf-8') as f:
        f.write(changelog)
    
    print(f"\nChangelog saved to: {changelog_file}")
    
    # Save release notes to file
    release_notes_file = git_log_file.parent / "RELEASE_NOTES.md"
    with open(release_notes_file, 'w', encoding='utf-8') as f:
        f.write(release_notes)
    
    print(f"Release notes saved to: {release_notes_file}")
    return 0

if __name__ == '__main__':
    sys.exit(main())

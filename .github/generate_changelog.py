#!/usr/bin/env python3
"""
Generate a changelog from git commits between the last two tags and post to Discord webhook.
"""

import subprocess
import re
import sys
import json
import argparse
from typing import List, Tuple, Optional


def run_git_command(args: List[str]) -> str:
    """Run a git command and return its output."""
    try:
        result = subprocess.run(
            ["git"] + args,
            capture_output=True,
            text=True,
            check=True
        )
        return result.stdout.strip()
    except subprocess.CalledProcessError as e:
        print(f"Git command failed: {e}", file=sys.stderr)
        sys.exit(1)


def get_last_two_tags() -> Tuple[str, str]:
    """Get the latest two git tags."""
    tags = run_git_command(["tag", "--sort=-version:refname"])
    tag_list = [t for t in tags.split("\n") if t]
    
    # Filter out old tags that start with 'v' (old versioning scheme)
    tag_list = [t for t in tag_list if not t.startswith('v')]

    if len(tag_list) < 2:
        print("Error: Need at least 2 tags in the repository", file=sys.stderr)
        sys.exit(1)
    
    return tag_list[0], tag_list[1]


def get_submodule_commit(submodule_path: str, tag: str) -> Optional[str]:
    """Get the commit hash of a submodule at a specific tag."""
    try:
        # Get the submodule commit at the specified tag
        result = run_git_command(["ls-tree", tag, submodule_path])
        # Format is: "<mode> commit <hash>\t<path>"
        parts = result.split()
        if len(parts) >= 3 and parts[1] == "commit":
            return parts[2]
        return None
    except:
        return None


def get_commits_between_tags(tag1: str, tag2: str) -> List[Tuple[str, str]]:
    """Get commits between two tags. Returns list of (message, author) tuples."""
    log_output = run_git_command([
        "log",
        f"{tag2}..{tag1}",
        "--format=%s|%an|%h"
    ])
    
    commits = []
    for line in log_output.split("\n"):
        if "|" in line:
            message, author, sha = line.split("|", 2)
            commits.append((message.strip(), author.strip(), sha.strip()))
    
    return commits


def filter_commits(commits: List[Tuple[str, str]], ignore_patterns: List[str]) -> List[Tuple[str, str]]:
    """Filter out commits matching any of the ignore patterns."""
    compiled_patterns = [re.compile(pattern) for pattern in ignore_patterns]
    
    filtered = []
    for message, author, sha in commits:
        if not any(pattern.search(message) for pattern in compiled_patterns):
            filtered.append((message, author, sha))
    
    return filtered


def generate_changelog(version: str, prev_version: str, commits: List[Tuple[str, str]], 
                      cs_commit_new: Optional[str], cs_commit_old: Optional[str]) -> str:
    """Generate markdown changelog."""
    # Calculate statistics
    commit_count = len(commits)
    unique_authors = len(set(author for _, author, _ in commits))
    
    changelog = f"# Dalamud Release v{version}\n\n"
    changelog += f"We just released Dalamud v{version}, which should be available to users within a few minutes. "
    changelog += f"This release includes **{commit_count} commit{'s' if commit_count != 1 else ''} from {unique_authors} contributor{'s' if unique_authors != 1 else ''}**.\n"
    changelog += f"[Click here](<https://github.com/goatcorp/Dalamud/compare/{prev_version}...{version}>) to see all Dalamud changes.\n\n"
    
    if cs_commit_new and cs_commit_old and cs_commit_new != cs_commit_old:
        changelog += f"It ships with an updated **FFXIVClientStructs [`{cs_commit_new[:7]}`](<https://github.com/aers/FFXIVClientStructs/commit/{cs_commit_new}>)**.\n"
        changelog += f"[Click here](<https://github.com/aers/FFXIVClientStructs/compare/{cs_commit_old}...{cs_commit_new}>) to see all CS changes.\n"
    elif cs_commit_new:
        changelog += f"It ships with **FFXIVClientStructs [`{cs_commit_new[:7]}`](<https://github.com/aers/FFXIVClientStructs/commit/{cs_commit_new}>)**.\n"
    
    changelog += "## Dalamud Changes\n\n"
    
    for message, author, sha in commits:
        changelog += f"* {message} (by **{author}** as [`{sha}`](<https://github.com/goatcorp/Dalamud/commit/{sha}>))\n"
    
    return changelog


def post_to_discord(webhook_url: str, content: str, version: str) -> None:
    """Post changelog to Discord webhook as a file attachment."""
    try:
        import requests
    except ImportError:
        print("Error: requests library is required. Install it with: pip install requests", file=sys.stderr)
        sys.exit(1)
    
    filename = f"changelog-v{version}.md"
    
    # Prepare the payload
    data = {
        "content": f"Dalamud v{version} has been released!",
        "attachments": [
            {
                "id": "0",
                "filename": filename
            }
        ]
    }
    
    # Prepare the files
    files = {
        "payload_json": (None, json.dumps(data)),
        "files[0]": (filename, content.encode('utf-8'), 'text/markdown')
    }
    
    try:
        result = requests.post(webhook_url, files=files)
        result.raise_for_status()
        print(f"Successfully posted to Discord webhook, code {result.status_code}")
    except requests.exceptions.HTTPError as err:
        print(f"Failed to post to Discord: {err}", file=sys.stderr)
        sys.exit(1)
    except Exception as e:
        print(f"Failed to post to Discord: {e}", file=sys.stderr)
        sys.exit(1)


def main():
    parser = argparse.ArgumentParser(
        description="Generate changelog from git commits and post to Discord webhook"
    )
    parser.add_argument(
        "--webhook-url",
        required=True,
        help="Discord webhook URL"
    )
    parser.add_argument(
        "--ignore",
        action="append",
        default=[],
        help="Regex patterns to ignore commits (can be specified multiple times)"
    )
    parser.add_argument(
        "--submodule-path",
        default="lib/FFXIVClientStructs",
        help="Path to the FFXIVClientStructs submodule (default: lib/FFXIVClientStructs)"
    )
    
    args = parser.parse_args()
    
    # Get the last two tags
    latest_tag, previous_tag = get_last_two_tags()
    print(f"Generating changelog between {previous_tag} and {latest_tag}")
    
    # Get submodule commits at both tags
    cs_commit_new = get_submodule_commit(args.submodule_path, latest_tag)
    cs_commit_old = get_submodule_commit(args.submodule_path, previous_tag)
    
    if cs_commit_new:
        print(f"FFXIVClientStructs commit (new): {cs_commit_new[:7]}")
    if cs_commit_old:
        print(f"FFXIVClientStructs commit (old): {cs_commit_old[:7]}")
    
    # Get commits between tags
    commits = get_commits_between_tags(latest_tag, previous_tag)
    print(f"Found {len(commits)} commits")
    
    # Filter commits
    filtered_commits = filter_commits(commits, args.ignore)
    print(f"After filtering: {len(filtered_commits)} commits")
    
    # Generate changelog
    changelog = generate_changelog(latest_tag, previous_tag, filtered_commits, 
                                   cs_commit_new, cs_commit_old)
    
    print("\n" + "="*50)
    print("Generated Changelog:")
    print("="*50)
    print(changelog)
    print("="*50 + "\n")
    
    # Post to Discord
    post_to_discord(args.webhook_url, changelog, latest_tag)


if __name__ == "__main__":
    main()
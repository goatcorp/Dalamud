#!/usr/bin/env python3
"""
Generate a changelog from git commits between the last two tags and post to Discord webhook.
"""

import subprocess
import re
import sys
import json
import argparse
import os
from typing import List, Tuple, Optional, Dict, Any


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


def get_repo_info() -> Tuple[str, str]:
    """Get repository owner and name from git remote."""
    try:
        remote_url = run_git_command(["config", "--get", "remote.origin.url"])

        # Handle both HTTPS and SSH URLs
        # SSH: git@github.com:owner/repo.git
        # HTTPS: https://github.com/owner/repo.git
        match = re.search(r'github\.com[:/](.+?)/(.+?)(?:\.git)?$', remote_url)
        if match:
            owner = match.group(1)
            repo = match.group(2)
            return owner, repo
        else:
            print("Error: Could not parse GitHub repository from remote URL", file=sys.stderr)
            sys.exit(1)
    except:
        print("Error: Could not get git remote URL", file=sys.stderr)
        sys.exit(1)


def get_commits_between_tags(tag1: str, tag2: str) -> List[str]:
    """Get commit SHAs between two tags."""
    log_output = run_git_command([
        "log",
        f"{tag2}..{tag1}",
        "--format=%H"
    ])

    commits = [sha.strip() for sha in log_output.split("\n") if sha.strip()]
    return commits


def get_pr_for_commit(commit_sha: str, owner: str, repo: str, token: str) -> Optional[Dict[str, Any]]:
    """Get PR information for a commit using GitHub API."""
    try:
        import requests
    except ImportError:
        print("Error: requests library is required. Install it with: pip install requests", file=sys.stderr)
        sys.exit(1)

    headers = {
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28"
    }

    if token:
        headers["Authorization"] = f"Bearer {token}"

    url = f"https://api.github.com/repos/{owner}/{repo}/commits/{commit_sha}/pulls"

    try:
        response = requests.get(url, headers=headers)
        response.raise_for_status()
        prs = response.json()

        if prs and len(prs) > 0:
            # Return the first PR (most relevant one)
            pr = prs[0]
            return {
                "number": pr["number"],
                "title": pr["title"],
                "author": pr["user"]["login"],
                "url": pr["html_url"]
            }
    except requests.exceptions.HTTPError as e:
        if e.response.status_code == 404:
            # Commit might not be associated with a PR
            return None
        elif e.response.status_code == 403:
            print("Warning: GitHub API rate limit exceeded. Consider providing a token.", file=sys.stderr)
            return None
        else:
            print(f"Warning: Failed to fetch PR for commit {commit_sha[:7]}: {e}", file=sys.stderr)
            return None
    except Exception as e:
        print(f"Warning: Error fetching PR for commit {commit_sha[:7]}: {e}", file=sys.stderr)
        return None

    return None


def get_prs_between_tags(tag1: str, tag2: str, owner: str, repo: str, token: str) -> List[Dict[str, Any]]:
    """Get PRs between two tags using GitHub API."""
    commits = get_commits_between_tags(tag1, tag2)
    print(f"Found {len(commits)} commits, fetching PR information...")

    prs = []
    seen_pr_numbers = set()

    for i, commit_sha in enumerate(commits, 1):
        if i % 10 == 0:
            print(f"Progress: {i}/{len(commits)} commits processed...")

        pr_info = get_pr_for_commit(commit_sha, owner, repo, token)
        if pr_info and pr_info["number"] not in seen_pr_numbers:
            seen_pr_numbers.add(pr_info["number"])
            prs.append(pr_info)

    return prs


def filter_prs(prs: List[Dict[str, Any]], ignore_patterns: List[str]) -> List[Dict[str, Any]]:
    """Filter out PRs matching any of the ignore patterns."""
    compiled_patterns = [re.compile(pattern) for pattern in ignore_patterns]

    filtered = []
    for pr in prs:
        if not any(pattern.search(pr["title"]) for pattern in compiled_patterns):
            filtered.append(pr)

    return filtered


def generate_changelog(version: str, prev_version: str, prs: List[Dict[str, Any]],
                      cs_commit_new: Optional[str], cs_commit_old: Optional[str],
                      owner: str, repo: str) -> str:
    """Generate markdown changelog."""
    # Calculate statistics
    pr_count = len(prs)
    unique_authors = len(set(pr["author"] for pr in prs))

    changelog = f"# Dalamud Release v{version}\n\n"
    changelog += f"We just released Dalamud v{version}, which should be available to users within a few minutes. "
    changelog += f"This release includes **{pr_count} PR{'s' if pr_count != 1 else ''} from {unique_authors} contributor{'s' if unique_authors != 1 else ''}**.\n"
    changelog += f"[Click here](<https://github.com/{owner}/{repo}/compare/{prev_version}...{version}>) to see all Dalamud changes.\n\n"

    if cs_commit_new and cs_commit_old and cs_commit_new != cs_commit_old:
        changelog += f"It ships with an updated **FFXIVClientStructs [`{cs_commit_new[:7]}`](<https://github.com/aers/FFXIVClientStructs/commit/{cs_commit_new}>)**.\n"
        changelog += f"[Click here](<https://github.com/aers/FFXIVClientStructs/compare/{cs_commit_old}...{cs_commit_new}>) to see all CS changes.\n"
    elif cs_commit_new:
        changelog += f"It ships with **FFXIVClientStructs [`{cs_commit_new[:7]}`](<https://github.com/aers/FFXIVClientStructs/commit/{cs_commit_new}>)**.\n"

    changelog += "## Dalamud Changes\n\n"

    for pr in prs:
        changelog += f"* {pr['title']} ([#**{pr['number']}**](<{pr['url']}>) by **{pr['author']}**)\n"

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
        "--github-token",
        default=os.environ.get("GITHUB_TOKEN"),
        help="GitHub API token (or set GITHUB_TOKEN env var). Increases rate limit."
    )
    parser.add_argument(
        "--ignore",
        action="append",
        default=[],
        help="Regex patterns to ignore PRs (can be specified multiple times)"
    )
    parser.add_argument(
        "--submodule-path",
        default="lib/FFXIVClientStructs",
        help="Path to the FFXIVClientStructs submodule (default: lib/FFXIVClientStructs)"
    )

    args = parser.parse_args()

    # Get repository info
    owner, repo = get_repo_info()
    print(f"Repository: {owner}/{repo}")

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

    # Get PRs between tags
    prs = get_prs_between_tags(latest_tag, previous_tag, owner, repo, args.github_token)
    prs.reverse()
    print(f"Found {len(prs)} PRs")

    # Filter PRs
    filtered_prs = filter_prs(prs, args.ignore)
    print(f"After filtering: {len(filtered_prs)} PRs")

    # Generate changelog
    changelog = generate_changelog(latest_tag, previous_tag, filtered_prs,
                                   cs_commit_new, cs_commit_old, owner, repo)

    print("\n" + "="*50)
    print("Generated Changelog:")
    print("="*50)
    print(changelog)
    print("="*50 + "\n")

    # Post to Discord
    post_to_discord(args.webhook_url, changelog, latest_tag)


if __name__ == "__main__":
    main()

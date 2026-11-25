# Installing PostgreSQL on Ubuntu 24.04 LTS (and newer LTS releases)

This guide installs the **latest stable PostgreSQL major version** from the official PostgreSQL Global Development Group (PGDG) apt repository, and lists everything you need around it.

> **Current status (November 2026):** PostgreSQL **19** was released on 2026-11-19. PostgreSQL **18** is the previous major. For production, many teams wait a few minor releases (19.1 or 19.2) before adopting a new major. Use 18 if you prefer that. Both are covered by the `PG_VERSION` variable below.

The steps work unchanged on Ubuntu 22.04, 24.04 and 26.04, because the repository script detects your release automatically.


## 1. Prerequisites

- Ubuntu 22.04, 24.04 or 26.04 (64-bit)
- A user with `sudo` privileges

Update the system and install the tools the setup needs:

```bash
sudo apt update && sudo apt upgrade -y
sudo apt install -y curl ca-certificates gnupg lsb-release
```


## 2. Add the official PostgreSQL repository

Ubuntu's own repositories only ship one PostgreSQL major version per Ubuntu release, and it is usually not the newest. The PGDG repository provides every supported major version and receives security fixes quickly.

The easiest and most future-proof method is the official helper script. It detects your Ubuntu codename and configures the signing key and repository for you.

```bash
sudo apt install -y postgresql-common
sudo /usr/share/postgresql-common/pgdg/apt.postgresql.org.sh
```

Press **Enter** when it asks to continue. Then refresh the package index:

```bash
sudo apt update
```

## 3. Install PostgreSQL

Set the major version once so you can copy-paste the commands below. Change *'{PG_VERSION}'* to `19` or `18` depending on the version you want to use.

```bash
sudo apt install -y postgresql-'{PG_VERSION}'
```

Or install whatever the newest major is. This tracks the latest version automatically:

```bash
sudo apt install -y postgresql
```

What gets installed:

| Package | Purpose |
|---|---|
| `postgresql-<version>` | The database server, including the standard contrib extensions |
| `postgresql-client-<version>` | `psql`, `pg_dump`, `pg_restore` and other client tools |
| `postgresql-common` | Cluster management tools (`pg_lsclusters`, `pg_createcluster`, `pg_upgradecluster`) |
| `libpq5` | Client library used by most language drivers |

Ubuntu automatically creates a default cluster named `main`, creates a `postgres` OS user, and starts and enables the service.


## 4. Verify the installation

```bash
# Service status
sudo systemctl status postgresql

# List clusters (version, port, status, data directory)
pg_lsclusters

# Server version
sudo -u postgres psql -c "SELECT version();"
```

Expected: a cluster `main` with status `online` on port `5432`.


## 5. Optional: Create your first user and database

Open a `psql` shell as the `postgres` superuser:

```bash
sudo -u postgres psql
```

Then run (replace the names and password):

```sql
CREATE ROLE myuser WITH LOGIN PASSWORD 'a-strong-password';
CREATE DATABASE mydb OWNER myuser;
\q
```

Connect with the new role:

```bash
psql -h localhost -U myuser -d mydb
```

> **Tip:** Using `-h localhost` makes `psql` connect over TCP with password authentication. Without it, `psql` uses a local socket with "peer" authentication, which expects your OS username to match the database role.

Optionally, set a password for the `postgres` superuser (only if you need password login for it):

```bash
sudo -u postgres psql -c "ALTER USER postgres PASSWORD 'another-strong-password';"
```

---

## 6. Optional: remote access

By default PostgreSQL only listens on `localhost`. **Skip this section for local development.**

In the following steps, change *'{PG_VERSION}'* to `19` or `18` depending on the version you choose to use.

**1. Listen on the network.** Edit `postgresql.conf`:

```bash
sudo nano /etc/postgresql/'{PG_VERSION}'/main/postgresql.conf
```

```conf
listen_addresses = '*'          # or a specific IP, e.g. 'localhost,192.168.1.10'
```

**2. Allow specific clients.** Edit `pg_hba.conf`:

```bash
sudo nano /etc/postgresql/'{PG_VERSION}'/main/pg_hba.conf
```

Add a line like this at the end (restrict it to your own subnet, never `0.0.0.0/0` unless you know what you are doing):

```conf
# TYPE  DATABASE  USER    ADDRESS           METHOD
host    mydb      myuser  192.168.1.0/24    scram-sha-256
```

**3. Restart:**

```bash
sudo systemctl restart postgresql
```

For connections over the public internet, enable TLS (`ssl = on` plus certificate settings in `postgresql.conf`) or put the database behind a VPN or SSH tunnel.

---

## 7. Optional: firewall (ufw)

Only needed if you enabled remote access.

```bash
# Allow PostgreSQL from one subnet only
sudo ufw allow from 192.168.1.0/24 to any port 5432 proto tcp

# Make sure SSH is allowed before enabling ufw on a remote server!
sudo ufw allow OpenSSH
sudo ufw enable
sudo ufw status
```

---

## 8. Optional: common extras

In the following steps, change *'{PG_VERSION}'* to `19` or `18` depending on the version you choose to use.

Install only what you need. Package names follow the pattern `postgresql-<version>-<extension>`.

```bash
# Spatial data
sudo apt install -y postgresql-'{PG_VERSION}'-postgis-3

# Vector search (AI / embeddings)
sudo apt install -y postgresql-'{PG_VERSION}'-pgvector

# Headers for building extensions or compiling drivers (e.g. psycopg2, pg gem)
sudo apt install -y postgresql-server-dev-'{PG_VERSION}' libpq-dev build-essential
```

Enable an extension inside a database:

```sql
CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS vector;
```

Extension packages can lag behind a brand-new major release. Check availability with:

```bash
apt-cache search postgresql-'{PG_VERSION}' | sort
```

**GUI clients:** pgAdmin ships from its own apt repository (see <https://www.pgadmin.org/download/pgadmin-4-apt/>). Alternatives such as DBeaver or DataGrip connect to PostgreSQL with no server-side setup.


## 9. Troubleshooting

| Problem | Fix |
|---|---|
| `E: Unable to locate package postgresql-19` | Re-run `sudo apt update`. Confirm the repo is present with `apt-cache policy postgresql-19`. If the version is brand new, packages for your release may not be published yet, so try `PG_VERSION=18`. |
| `psql: FATAL: Peer authentication failed` | Use `-h localhost` for password login, or run `sudo -u postgres psql`. |
| `psql: FATAL: password authentication failed` | Reset it: `sudo -u postgres psql -c "ALTER USER myuser PASSWORD 'new';"` |
| Can't connect remotely | Check `listen_addresses`, the `pg_hba.conf` entry, the firewall, and restart the service. Test with `ss -tlnp \| grep 5432`. |
| Port 5432 already in use | A second cluster may exist. Run `pg_lsclusters` and stop or drop the unneeded one. |
| Service won't start | Read the log: `sudo tail -n 50 /var/log/postgresql/postgresql-${PG_VERSION}-main.log` |
| Check service logs | `journalctl -u postgresql@${PG_VERSION}-main -e` |

---

## References

- PostgreSQL Linux downloads: <https://www.postgresql.org/download/linux/ubuntu/>
- Documentation: <https://www.postgresql.org/docs/>
- Versioning and support policy: <https://www.postgresql.org/support/versioning/>
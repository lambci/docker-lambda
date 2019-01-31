FROM lambci/lambda-base

ENV PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/opt/bin

# A couple of packages are either missing critical-ish files, or didn't make it into the tar
# Reinstalling filesystem might not succeed fully, but continue anyway
RUN chmod 1777 /tmp && \
  yum install -y --releasever=latest glibc-devel; \
  /usr/bin/python3 -c "from configparser import SafeConfigParser; \
yum_conf = SafeConfigParser(); \
yum_conf.read('/etc/yum/yum.conf'); \
yum_conf.has_section('main') or yum_conf.add_section('main'); \
yum_conf.set('main', 'plugins', '1'); \
f = open('/etc/yum/yum.conf', 'w'); \
yum_conf.write(f); \
f.close();" && \
  rpm --rebuilddb && \
  yum install -y yum-plugin-ovl && \
  yum reinstall -y setup pam shadow-utils audit-libs glibc glibc-common openssl iptables && \
  yum groupinstall -y development && \
  yum install -y clang cmake docker \
    libssh2-devel libffi-devel python27-devel python34-devel \
    libmpc-devel mpfr-devel gmp-devel pcre-devel cairo-devel ImageMagick-devel \
    libcurl-devel expat-devel libicu-devel libxml2-devel libxslt-devel lua-devel \
    ncurses-devel readline-devel db4-devel gdbm-devel sqlite-devel && \
  rm -rf /var/cache/yum /var/lib/rpm/__db.* && \
  > /var/log/yum.log


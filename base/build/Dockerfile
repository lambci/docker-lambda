FROM lambci/lambda-base

ENV PATH=/root/.local/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/opt/bin

# A couple of packages are either missing critical-ish files, or didn't make it into the tar
RUN chmod 1777 /tmp && \
  /usr/bin/python3 -c "from configparser import SafeConfigParser; \
yum_conf = SafeConfigParser(); \
yum_conf.read('/etc/yum.conf'); \
yum_conf.has_section('main') or yum_conf.add_section('main'); \
yum_conf.set('main', 'plugins', '1'); \
f = open('/etc/yum.conf', 'w'); \
yum_conf.write(f); \
f.close();" && \
  rpm --rebuilddb && \
  yum install -y yum-plugin-ovl && \
  yum reinstall -y setup pam shadow-utils audit-libs openssl glibc-common glibc && \
  yum groupinstall -y development && \
  yum install -y clang cmake docker python27-devel python36-devel \
    ImageMagick-devel-6.7.8.9 cairo-devel libssh2-devel libxslt-devel libmpc-devel readline-devel db4-devel \
    libffi-devel expat-devel libicu-devel lua-devel gdbm-devel sqlite-devel pcre-devel libcurl-devel && \
  alternatives --set gcc /usr/bin/gcc48 && \
  alternatives --set g++ /usr/bin/g++48 && \
  alternatives --set cpp /usr/bin/cpp48 && \
  python3 -m pip install -U pip setuptools wheel --no-cache-dir && \
  pip install --user pipx --no-cache-dir && \
  pipx run awscli==1.* 2>/dev/null || true && \
  yum clean all && \
  rm -rf /var/cache/yum
